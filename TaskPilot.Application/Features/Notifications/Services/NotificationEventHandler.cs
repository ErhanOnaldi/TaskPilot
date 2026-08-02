using TaskPilot.Application.Events;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Notifications;
using TaskPilot.Application.Interfaces.Persistence.Tasks;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Notifications.Services;

public sealed class NotificationEventHandler(
    INotificationRepository notificationRepository,
    ITaskRepository taskRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock) : INotificationEventHandler
{
    public Task HandleAsync(TaskCreatedEvent taskCreatedEvent, CancellationToken cancellationToken)
    {
        if (!taskCreatedEvent.AssignedUserId.HasValue ||
            taskCreatedEvent.AssignedUserId.Value == taskCreatedEvent.CreatedByUserId)
        {
            return Task.CompletedTask;
        }

        return CreateNotificationIfNotExistsAsync(
            userId: taskCreatedEvent.AssignedUserId.Value,
            sourceEventId: taskCreatedEvent.EventId,
            type: "TaskCreated",
            title: "Task created",
            message: "A task was created and assigned to you.",
            relatedEntityId: taskCreatedEvent.TaskId,
            cancellationToken);
    }

    public Task HandleAsync(TaskAssignedEvent taskAssignedEvent, CancellationToken cancellationToken)
    {
        if (taskAssignedEvent.AssignedUserId == taskAssignedEvent.AssignedByUserId)
        {
            return Task.CompletedTask;
        }

        return CreateNotificationIfNotExistsAsync(
            userId: taskAssignedEvent.AssignedUserId,
            sourceEventId: taskAssignedEvent.EventId,
            type: "TaskAssigned",
            title: "Task assigned",
            message: "A task was assigned to you.",
            relatedEntityId: taskAssignedEvent.TaskId,
            cancellationToken);
    }

    public Task HandleAsync(WorkspaceMemberInvitedEvent workspaceMemberInvitedEvent, CancellationToken cancellationToken)
    {
        return CreateNotificationIfNotExistsAsync(
            userId: workspaceMemberInvitedEvent.InvitedUserId,
            sourceEventId: workspaceMemberInvitedEvent.EventId,
            type: "WorkspaceMemberInvited",
            title: "Workspace invitation",
            message: "You were added to a workspace.",
            relatedEntityId: workspaceMemberInvitedEvent.WorkspaceId,
            cancellationToken);
    }

    public async Task HandleAsync(CommentAddedEvent commentAddedEvent, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(commentAddedEvent.TaskId);
        if (task is null)
        {
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
        var exists = await notificationRepository.ExistsBySourceEventIdAsync(userId, sourceEventId, cancellationToken);
        if (exists)
        {
            return;
        }

        var now = clock.UtcNow;
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
}
