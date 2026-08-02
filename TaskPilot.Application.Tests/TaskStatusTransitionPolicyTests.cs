using TaskPilot.Domain.Entities;
using TaskPilot.Domain.Policies;

namespace TaskPilot.Application.Tests;

public sealed class TaskStatusTransitionPolicyTests
{
    [Theory]
    [InlineData(TaskItemStatus.Todo, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.InReview)]
    [InlineData(TaskItemStatus.InReview, TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.InProgress)]
    [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.InProgress)]
    public void IsAllowed_returns_true_for_prd_transitions(TaskItemStatus current, TaskItemStatus requested)
    {
        Assert.True(TaskStatusTransitionPolicy.IsAllowed(current, requested));
    }

    [Theory]
    [InlineData(TaskItemStatus.Todo, TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.InProgress, TaskItemStatus.Done)]
    [InlineData(TaskItemStatus.Done, TaskItemStatus.Todo)]
    [InlineData(TaskItemStatus.Cancelled, TaskItemStatus.Done)]
    public void IsAllowed_returns_false_for_transitions_outside_prd_matrix(TaskItemStatus current, TaskItemStatus requested)
    {
        Assert.False(TaskStatusTransitionPolicy.IsAllowed(current, requested));
    }
}
