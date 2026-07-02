using MassTransit;
using TaskPilot.Application.Events;
using TaskPilot.Application.Features.Notifications.Services;

namespace TaskPilot.Infrastructure.Messaging.Consumers;

public sealed class NotificationConsumer(INotificationEventHandler notificationEventHandler) :
    IConsumer<TaskCreatedEvent>,
    IConsumer<TaskAssignedEvent>,
    IConsumer<CommentAddedEvent>
{
    public Task Consume(ConsumeContext<TaskCreatedEvent> context)
    {
        return notificationEventHandler.HandleAsync(context.Message, context.CancellationToken);
    }

    public Task Consume(ConsumeContext<TaskAssignedEvent> context)
    {
        return notificationEventHandler.HandleAsync(context.Message, context.CancellationToken);
    }

    public Task Consume(ConsumeContext<CommentAddedEvent> context)
    {
        return notificationEventHandler.HandleAsync(context.Message, context.CancellationToken);
    }
}
