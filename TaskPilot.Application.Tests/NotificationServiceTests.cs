using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Notifications.Dtos;
using TaskPilot.Application.Features.Notifications.Services;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Notifications;
using TaskPilot.Application.Mappings;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Tests;

public class NotificationServiceTests
{
    [Fact]
    public async Task GetNotificationsAsync_returns_paged_notifications_for_current_user()
    {
        var repository = new FakeNotificationRepository();
        repository.Notifications.AddRange(
        [
            new Notification { Id = 1, UserId = 42, Type = "TaskAssigned", Title = "Old", Message = "Old", CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new Notification { Id = 2, UserId = 42, Type = "TaskAssigned", Title = "New", Message = "New", CreatedAt = DateTime.UtcNow },
            new Notification { Id = 3, UserId = 9, Type = "TaskAssigned", Title = "Other", Message = "Other", CreatedAt = DateTime.UtcNow.AddDays(1) }
        ]);
        var service = CreateService(repository, currentUserId: 42);

        var result = await service.GetNotificationsAsync(
            new NotificationQueryParameters { PageNumber = 1, PageSize = 1 },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.TotalCount);
        Assert.Equal(2, result.Data.TotalPages);
        Assert.True(result.Data.HasNextPage);
        var notification = Assert.Single(result.Data.Items);
        Assert.Equal(2, notification.Id);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_updates_only_current_users_unread_notifications()
    {
        var repository = new FakeNotificationRepository();
        repository.Notifications.AddRange(
        [
            new Notification { Id = 1, UserId = 42, Type = "TaskAssigned", Title = "Unread", Message = "Unread", IsRead = false },
            new Notification { Id = 2, UserId = 42, Type = "TaskAssigned", Title = "Read", Message = "Read", IsRead = true },
            new Notification { Id = 3, UserId = 9, Type = "TaskAssigned", Title = "Other", Message = "Other", IsRead = false }
        ]);
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, currentUserId: 42, unitOfWork);

        var result = await service.MarkAllAsReadAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.Notifications.Single(notification => notification.Id == 1).IsRead);
        Assert.True(repository.Notifications.Single(notification => notification.Id == 2).IsRead);
        Assert.False(repository.Notifications.Single(notification => notification.Id == 3).IsRead);
        Assert.Equal(1, repository.MarkAllAsReadCallCount);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    private static NotificationService CreateService(
        FakeNotificationRepository repository,
        int currentUserId,
        FakeUnitOfWork? unitOfWork = null)
    {
        return new NotificationService(
            repository,
            unitOfWork ?? new FakeUnitOfWork(),
            new FakeCurrentUserService(currentUserId),
            CreateMapper());
    }

    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(
            configuration => configuration.AddProfile<ApplicationMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
    }

    private sealed class FakeCurrentUserService(int userId) : ICurrentUserService
    {
        public int? UserId { get; } = userId;
        public bool IsAuthenticated => true;
        public int GetRequiredUserId() => userId;
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
        public int MarkAllAsReadCallCount { get; private set; }

        public Task<PagedResponse<Notification>> GetNotificationsByUserIdAsync(
            int userId,
            NotificationQueryParameters query,
            CancellationToken cancellationToken)
        {
            var notifications = Notifications
                .Where(notification => notification.UserId == userId)
                .Where(notification => !query.IsRead.HasValue || notification.IsRead == query.IsRead.Value)
                .OrderByDescending(notification => notification.CreatedAt)
                .ThenByDescending(notification => notification.Id)
                .ToList();

            var pageItems = notifications
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return Task.FromResult(PagedResponse<Notification>.Create(pageItems, query.PageNumber, query.PageSize, notifications.Count));
        }

        public Task<List<Notification>> GetUnreadNotificationsByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications.Where(notification => notification.UserId == userId && !notification.IsRead).ToList());
        }

        public Task<int> MarkAllAsReadAsync(int userId, DateTime utcNow, CancellationToken cancellationToken)
        {
            MarkAllAsReadCallCount++;
            var notifications = Notifications.Where(notification => notification.UserId == userId && !notification.IsRead).ToList();
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.UpdatedAt = utcNow;
            }

            return Task.FromResult(notifications.Count);
        }

        public Task<bool> ExistsBySourceEventIdAsync(int userId, Guid sourceEventId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Notifications.Any(notification =>
                notification.UserId == userId &&
                notification.SourceEventId == sourceEventId));
        }

        public Task<List<Notification>> GetAllAsync() => Task.FromResult(Notifications);
        public Task<List<Notification>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(Notifications.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList());
        public IQueryable<Notification> Where(Expression<Func<Notification, bool>> predicate) => Notifications.AsQueryable().Where(predicate);
        public ValueTask<Notification?> GetByIdAsync(int id) => ValueTask.FromResult(Notifications.FirstOrDefault(notification => notification.Id == id));
        public ValueTask AddAsync(Notification entity) { Notifications.Add(entity); return ValueTask.CompletedTask; }
        public Task<bool> AnyAsync(Expression<Func<Notification, bool>> predicate) => Task.FromResult(Notifications.AsQueryable().Any(predicate));
        public void Update(Notification entity) { }
        public void Delete(Notification entity) => Notifications.Remove(entity);
    }
}
