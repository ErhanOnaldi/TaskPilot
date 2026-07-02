using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TaskPilot.Application.Events;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;

namespace TaskPilot.Infrastructure.Messaging;

public class RabbitMqEventPublisher(IOptions<RabbitMqOptions> options) : IEventPublisher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
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
        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(
            exchange: rabbitMqOptions.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);
        
        var json = JsonSerializer.Serialize(@event);
        var body = Encoding.UTF8.GetBytes(json);
        var routingKey = GetRoutingKey<TEvent>();
        await channel.BasicPublishAsync(
            exchange: rabbitMqOptions.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            body: body,
            cancellationToken: cancellationToken);
    }

    private static string GetRoutingKey<TEvent>()
    {
        return typeof(TEvent) switch
        {
            var eventType when eventType == typeof(TaskCreatedEvent) => "task.created",
            var eventType when eventType == typeof(TaskAssignedEvent) => "task.assigned",
            var eventType when eventType == typeof(CommentAddedEvent) => "comment.added",
            _ => throw new InvalidOperationException($"No RabbitMQ routing key configured for event type {typeof(TEvent).Name}.")
        };
    }
}
