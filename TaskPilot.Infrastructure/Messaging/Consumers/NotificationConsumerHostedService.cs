using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TaskPilot.Application.Events;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Notifications;
using TaskPilot.Application.Interfaces.Persistence.Tasks;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Infrastructure.Messaging.Consumers;

public sealed class NotificationConsumerHostedService(
    IOptions<RabbitMqOptions> options,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<NotificationConsumerHostedService> logger) : BackgroundService
{
    private const string QueueName = "taskpilot.notifications";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitMqOptions = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = rabbitMqOptions.HostName,
            Port = rabbitMqOptions.Port,
            UserName = rabbitMqOptions.UserName,
            Password = rabbitMqOptions.Password,
            VirtualHost = rabbitMqOptions.VirtualHost
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            exchange: rabbitMqOptions.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        await BindQueueAsync(channel, rabbitMqOptions.ExchangeName, stoppingToken);
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 10,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            await HandleDeliveryAsync(channel, eventArgs, stoppingToken);
        };

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation("Notification RabbitMQ consumer started. Queue: {QueueName}", QueueName);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Notification RabbitMQ consumer is stopping.");
        }
    }

    private static async Task BindQueueAsync(IChannel channel, string exchangeName, CancellationToken cancellationToken)
    {
        await channel.QueueBindAsync(QueueName, exchangeName, "task.created", cancellationToken: cancellationToken);
        await channel.QueueBindAsync(QueueName, exchangeName, "task.assigned", cancellationToken: cancellationToken);
        await channel.QueueBindAsync(QueueName, exchangeName, "comment.added", cancellationToken: cancellationToken);
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = Encoding.UTF8.GetString(eventArgs.Body.Span);
            await HandleMessageAsync(eventArgs.RoutingKey, message, cancellationToken);

            await channel.BasicAckAsync(
                deliveryTag: eventArgs.DeliveryTag,
                multiple: false,
                cancellationToken: cancellationToken);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "Invalid RabbitMQ message received. RoutingKey: {RoutingKey}", eventArgs.RoutingKey);
            await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "RabbitMQ message processing failed. RoutingKey: {RoutingKey}", eventArgs.RoutingKey);
            await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true, cancellationToken);
        }
    }

    private async Task HandleMessageAsync(string routingKey, string message, CancellationToken cancellationToken)
    {
        switch (routingKey)
        {
            case "task.created":
                await HandleTaskCreatedAsync(Deserialize<TaskCreatedEvent>(message), cancellationToken);
                break;
            case "task.assigned":
                await HandleTaskAssignedAsync(Deserialize<TaskAssignedEvent>(message), cancellationToken);
                break;
            case "comment.added":
                await HandleCommentAddedAsync(Deserialize<CommentAddedEvent>(message), cancellationToken);
                break;
            default:
                logger.LogWarning("Unknown RabbitMQ routing key ignored. RoutingKey: {RoutingKey}", routingKey);
                break;
        }
    }

    private async Task HandleTaskCreatedAsync(TaskCreatedEvent taskCreatedEvent, CancellationToken cancellationToken)
    {
        if (!taskCreatedEvent.AssignedUserId.HasValue ||
            taskCreatedEvent.AssignedUserId.Value == taskCreatedEvent.CreatedByUserId)
        {
            return;
        }

        await CreateNotificationIfNotExistsAsync(
            userId: taskCreatedEvent.AssignedUserId.Value,
            sourceEventId: taskCreatedEvent.EventId,
            type: "TaskCreated",
            title: "Task created",
            message: "A task was created and assigned to you.",
            relatedEntityId: taskCreatedEvent.TaskId,
            cancellationToken);
    }

    private async Task HandleTaskAssignedAsync(TaskAssignedEvent taskAssignedEvent, CancellationToken cancellationToken)
    {
        if (taskAssignedEvent.AssignedUserId == taskAssignedEvent.AssignedByUserId)
        {
            return;
        }

        await CreateNotificationIfNotExistsAsync(
            userId: taskAssignedEvent.AssignedUserId,
            sourceEventId: taskAssignedEvent.EventId,
            type: "TaskAssigned",
            title: "Task assigned",
            message: "A task was assigned to you.",
            relatedEntityId: taskAssignedEvent.TaskId,
            cancellationToken);
    }

    private async Task HandleCommentAddedAsync(CommentAddedEvent commentAddedEvent, CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var taskRepository = scope.ServiceProvider.GetRequiredService<ITaskRepository>();
        var task = await taskRepository.GetByIdAsync(commentAddedEvent.TaskId);
        if (task is null)
        {
            logger.LogWarning("CommentAddedEvent ignored because task was not found. TaskId: {TaskId}", commentAddedEvent.TaskId);
            return;
        }

        var recipientUserIds = new[] { task.CreatedByUserId, task.AssignedUserId }
            .Where(userId => userId.HasValue && userId.Value != commentAddedEvent.AuthorUserId)
            .Select(userId => userId!.Value)
            .Distinct();

        foreach (var recipientUserId in recipientUserIds)
        {
            await CreateNotificationIfNotExistsAsync(
                userId: recipientUserId,
                sourceEventId: commentAddedEvent.EventId,
                type: "CommentAdded",
                title: "Comment added",
                message: "A comment was added to a task you follow.",
                relatedEntityId: commentAddedEvent.TaskId,
                cancellationToken);
        }
    }

    private async Task CreateNotificationIfNotExistsAsync(
        int userId,
        Guid sourceEventId,
        string type,
        string title,
        string message,
        int relatedEntityId,
        CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var exists = await notificationRepository.ExistsBySourceEventIdAsync(userId, sourceEventId, cancellationToken);
        if (exists)
        {
            return;
        }

        var now = DateTime.UtcNow;
        await notificationRepository.AddAsync(new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityId = relatedEntityId,
            SourceEventId = sourceEventId,
            CreatedAt = now,
            UpdatedAt = now
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static TEvent Deserialize<TEvent>(string message)
    {
        return JsonSerializer.Deserialize<TEvent>(message, JsonOptions)
               ?? throw new JsonException($"RabbitMQ message could not be deserialized as {typeof(TEvent).Name}.");
    }
}
