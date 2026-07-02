using TaskPilot.Application.Events;

namespace TaskPilot.Application.Features.Notifications.Services;

public interface INotificationEventHandler
{
    Task HandleAsync(TaskCreatedEvent taskCreatedEvent, CancellationToken cancellationToken);
    Task HandleAsync(TaskAssignedEvent taskAssignedEvent, CancellationToken cancellationToken);
    Task HandleAsync(CommentAddedEvent commentAddedEvent, CancellationToken cancellationToken);
}
