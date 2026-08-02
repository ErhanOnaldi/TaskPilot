using TaskPilot.Domain.Entities;

namespace TaskPilot.Domain.Policies;

/// <summary>
/// Defines the permitted lifecycle transitions for a task. Authorization to perform
/// a permitted transition remains the responsibility of the application layer.
/// </summary>
public static class TaskStatusTransitionPolicy
{
    public static bool IsAllowed(TaskItemStatus currentStatus, TaskItemStatus requestedStatus)
    {
        return (currentStatus, requestedStatus) switch
        {
            (TaskItemStatus.Todo, TaskItemStatus.InProgress) => true,
            (TaskItemStatus.Todo, TaskItemStatus.Cancelled) => true,
            (TaskItemStatus.InProgress, TaskItemStatus.InReview) => true,
            (TaskItemStatus.InProgress, TaskItemStatus.Cancelled) => true,
            (TaskItemStatus.InReview, TaskItemStatus.Done) => true,
            (TaskItemStatus.InReview, TaskItemStatus.Cancelled) => true,
            (TaskItemStatus.Done, TaskItemStatus.InProgress) => true,
            (TaskItemStatus.Cancelled, TaskItemStatus.InProgress) => true,
            _ => false
        };
    }

    public static bool IsCancelledReopen(TaskItemStatus currentStatus, TaskItemStatus requestedStatus)
    {
        return currentStatus == TaskItemStatus.Cancelled && requestedStatus == TaskItemStatus.InProgress;
    }
}
