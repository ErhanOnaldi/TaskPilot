using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Authorization.Results;
using TaskPilot.Application.Features.Comments.Services;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Comments;
using TaskPilot.Application.Mappings;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Tests;

public class CommentServiceTests
{
    [Fact]
    public async Task GetCommentsAsync_returns_comments_ordered_by_created_at()
    {
        var commentRepository = new FakeCommentRepository();
        commentRepository.Comments.AddRange(
        [
            new Comment { Id = 2, TaskId = 10, UserId = 1, Content = "Second", CreatedAt = DateTime.UtcNow.AddMinutes(2) },
            new Comment { Id = 1, TaskId = 10, UserId = 1, Content = "First", CreatedAt = DateTime.UtcNow.AddMinutes(1) },
            new Comment { Id = 3, TaskId = 11, UserId = 1, Content = "Other", CreatedAt = DateTime.UtcNow }
        ]);
        var taskRepository = new FakeTaskRepository();
        taskRepository.Tasks.Add(new TaskItem { Id = 10, ProjectId = 20, Title = "Task", CreatedByUserId = 1 });
        var service = CreateService(commentRepository, taskRepository);

        var result = await service.GetCommentsAsync(10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal([1, 2], result.Data.Select(comment => comment.Id));
        Assert.Equal(10, commentRepository.LastTaskId);
    }

    private static CommentService CreateService(
        FakeCommentRepository commentRepository,
        FakeTaskRepository taskRepository)
    {
        return new CommentService(
            commentRepository,
            taskRepository,
            new FakeUnitOfWork(),
            new FakeAccessControlService(),
            CreateMapper(),
            new FakeEventPublisher());
    }

    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(
            configuration => configuration.AddProfile<ApplicationMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
    }

    private sealed class FakeCommentRepository : ICommentRepository
    {
        public List<Comment> Comments { get; } = [];
        public int LastTaskId { get; private set; }

        public Task<List<Comment>> GetCommentsByTaskIdAsync(int taskId, CancellationToken cancellationToken)
        {
            LastTaskId = taskId;
            return Task.FromResult(Comments
                .Where(comment => comment.TaskId == taskId)
                .OrderBy(comment => comment.CreatedAt)
                .ToList());
        }

        public Task<List<Comment>> GetAllAsync() => Task.FromResult(Comments);
        public Task<List<Comment>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(Comments);
        public IQueryable<Comment> Where(Expression<Func<Comment, bool>> predicate) => Comments.AsQueryable().Where(predicate);
        public ValueTask<Comment?> GetByIdAsync(int id) => ValueTask.FromResult(Comments.FirstOrDefault(comment => comment.Id == id));
        public ValueTask AddAsync(Comment entity) { Comments.Add(entity); return ValueTask.CompletedTask; }
        public Task<bool> AnyAsync(Expression<Func<Comment, bool>> predicate) => Task.FromResult(Comments.AsQueryable().Any(predicate));
        public void Update(Comment entity) { }
        public void Delete(Comment entity) => Comments.Remove(entity);
    }

    private sealed class FakeTaskRepository : IGenericRepository<TaskItem>
    {
        public List<TaskItem> Tasks { get; } = [];

        public Task<List<TaskItem>> GetAllAsync() => Task.FromResult(Tasks);
        public Task<List<TaskItem>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(Tasks);
        public IQueryable<TaskItem> Where(Expression<Func<TaskItem, bool>> predicate) => Tasks.AsQueryable().Where(predicate);
        public ValueTask<TaskItem?> GetByIdAsync(int id) => ValueTask.FromResult(Tasks.FirstOrDefault(task => task.Id == id));
        public ValueTask AddAsync(TaskItem entity) { Tasks.Add(entity); return ValueTask.CompletedTask; }
        public Task<bool> AnyAsync(Expression<Func<TaskItem, bool>> predicate) => Task.FromResult(Tasks.AsQueryable().Any(predicate));
        public void Update(TaskItem entity) { }
        public void Delete(TaskItem entity) => Tasks.Remove(entity);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeEventPublisher : IEventPublisher
    {
        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAccessControlService : IAccessControlService
    {
        public Task<WorkspaceAccessResult> AuthorizeWorkspaceAsync(
            int workspaceId,
            WorkspaceAccessLevel accessLevel,
            bool requireActiveWorkspace,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ProjectAccessResult> AuthorizeProjectAsync(
            int projectId,
            ProjectAccessLevel accessLevel,
            bool requireActiveProject,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new ProjectAccessResult(null!, null!, null!, 1, null));
        }
    }
}
