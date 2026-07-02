using System.Linq.Expressions;
using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Events;
using TaskPilot.Application.Features.Notifications.Dtos;
using TaskPilot.Application.Features.Notifications.Services;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Notifications;
using TaskPilot.Application.Interfaces.Persistence.Tasks;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Tests;

public class NotificationEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_TaskAssignedEvent_creates_notification_for_assigned_user()
    {
        var notifications = new FakeNotificationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(notifications, new FakeTaskRepository(), unitOfWork);
        var eventId = Guid.NewGuid();

        await handler.HandleAsync(
            new TaskAssignedEvent(eventId, TaskId: 10, ProjectId: 20, AssignedUserId: 2, AssignedByUserId: 1, DateTime.UtcNow),
            CancellationToken.None);

        var notification = Assert.Single(notifications.Notifications);
        Assert.Equal(2, notification.UserId);
        Assert.Equal("TaskAssigned", notification.Type);
        Assert.Equal(10, notification.RelatedEntityId);
        Assert.Equal(eventId, notification.SourceEventId);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_TaskAssignedEvent_does_not_create_notification_when_user_assigned_task_to_self()
    {
        var notifications = new FakeNotificationRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(notifications, new FakeTaskRepository(), unitOfWork);

        await handler.HandleAsync(
            new TaskAssignedEvent(Guid.NewGuid(), TaskId: 10, ProjectId: 20, AssignedUserId: 2, AssignedByUserId: 2, DateTime.UtcNow),
            CancellationToken.None);

        Assert.Empty(notifications.Notifications);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_duplicate_source_event_does_not_create_second_notification()
    {
        var eventId = Guid.NewGuid();
        var notifications = new FakeNotificationRepository();
        notifications.Notifications.Add(new Notification
        {
            UserId = 2,
            Type = "TaskAssigned",
            Title = "Existing",
            Message = "Existing",
            SourceEventId = eventId
        });
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(notifications, new FakeTaskRepository(), unitOfWork);

        await handler.HandleAsync(
            new TaskAssignedEvent(eventId, TaskId: 10, ProjectId: 20, AssignedUserId: 2, AssignedByUserId: 1, DateTime.UtcNow),
            CancellationToken.None);

        Assert.Single(notifications.Notifications);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task HandleAsync_CommentAddedEvent_notifies_creator_and_assignee_except_author()
    {
        var notifications = new FakeNotificationRepository();
        var tasks = new FakeTaskRepository();
        tasks.Tasks.Add(new TaskItem
        {
            Id = 10,
            ProjectId = 20,
            Title = "Task",
            CreatedByUserId = 1,
            AssignedUserId = 2
        });
        var handler = CreateHandler(notifications, tasks, new FakeUnitOfWork());
        var eventId = Guid.NewGuid();

        await handler.HandleAsync(
            new CommentAddedEvent(eventId, CommentId: 100, TaskId: 10, ProjectId: 20, AuthorUserId: 2, DateTime.UtcNow),
            CancellationToken.None);

        var notification = Assert.Single(notifications.Notifications);
        Assert.Equal(1, notification.UserId);
        Assert.Equal("CommentAdded", notification.Type);
        Assert.Equal(10, notification.RelatedEntityId);
        Assert.Equal(eventId, notification.SourceEventId);
    }

    [Fact]
    public async Task HandleAsync_CommentAddedEvent_deduplicates_same_creator_and_assignee()
    {
        var notifications = new FakeNotificationRepository();
        var tasks = new FakeTaskRepository();
        tasks.Tasks.Add(new TaskItem
        {
            Id = 10,
            ProjectId = 20,
            Title = "Task",
            CreatedByUserId = 1,
            AssignedUserId = 1
        });
        var handler = CreateHandler(notifications, tasks, new FakeUnitOfWork());

        await handler.HandleAsync(
            new CommentAddedEvent(Guid.NewGuid(), CommentId: 100, TaskId: 10, ProjectId: 20, AuthorUserId: 2, DateTime.UtcNow),
            CancellationToken.None);

        var notification = Assert.Single(notifications.Notifications);
        Assert.Equal(1, notification.UserId);
    }

    private static NotificationEventHandler CreateHandler(
        FakeNotificationRepository notifications,
        FakeTaskRepository tasks,
        FakeUnitOfWork unitOfWork)
    {
        return new NotificationEventHandler(notifications, tasks, unitOfWork);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        public List<Notification> Notifications { get; } = [];

        public Task<bool> ExistsBySourceEventIdAsync(int userId, Guid sourceEventId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications.Any(notification =>
                notification.UserId == userId &&
                notification.SourceEventId == sourceEventId));
        }

        public ValueTask AddAsync(Notification entity)
        {
            Notifications.Add(entity);
            return ValueTask.CompletedTask;
        }

        public Task<PagedResponse<Notification>> GetNotificationsByUserIdAsync(int userId, NotificationQueryParameters query, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<List<Notification>> GetUnreadNotificationsByUserIdAsync(int userId, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<int> MarkAllAsReadAsync(int userId, DateTime utcNow, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<List<Notification>> GetAllAsync() => throw new NotSupportedException();
        public Task<List<Notification>> GetAllPagedAsync(int pageNumber, int pageSize) => throw new NotSupportedException();
        public IQueryable<Notification> Where(Expression<Func<Notification, bool>> predicate) => throw new NotSupportedException();
        public ValueTask<Notification?> GetByIdAsync(int id) => throw new NotSupportedException();
        public Task<bool> AnyAsync(Expression<Func<Notification, bool>> predicate) => throw new NotSupportedException();
        public void Update(Notification entity) => throw new NotSupportedException();
        public void Delete(Notification entity) => throw new NotSupportedException();
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        public List<TaskItem> Tasks { get; } = [];

        public ValueTask<TaskItem?> GetByIdAsync(int id)
        {
            return ValueTask.FromResult(Tasks.FirstOrDefault(task => task.Id == id));
        }

        public Task<PagedResponse<TaskItem>> GetTasksByProjectIdAsync(int projectId, TaskQueryParameters query, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public Task<List<TaskItem>> GetAllAsync() => throw new NotSupportedException();
        public Task<List<TaskItem>> GetAllPagedAsync(int pageNumber, int pageSize) => throw new NotSupportedException();
        public IQueryable<TaskItem> Where(Expression<Func<TaskItem, bool>> predicate) => throw new NotSupportedException();
        public ValueTask AddAsync(TaskItem entity) => throw new NotSupportedException();
        public Task<bool> AnyAsync(Expression<Func<TaskItem, bool>> predicate) => throw new NotSupportedException();
        public void Update(TaskItem entity) => throw new NotSupportedException();
        public void Delete(TaskItem entity) => throw new NotSupportedException();
    }
}
