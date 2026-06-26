using System.Linq.Expressions;
using System.Net;
using TaskPilot.Application;
using TaskPilot.Application.Features.Workspace.Dtos;
using TaskPilot.Application.Features.Workspace.Services;
using TaskPilot.Application.Features.Workspace.Validators;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Tests;

public class WorkspaceServiceTests
{
    [Fact]
    public async Task CreateWorkspaceAsync_adds_current_user_as_owner_member()
    {
        var repository = new FakeWorkspaceRepository();
        var service = CreateService(repository, currentUserId: 42);

        var result = await service.CreateWorkspaceAsync(new CreateWorkspaceRequest("Engineering"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var workspace = Assert.Single(repository.Workspaces);
        Assert.Equal("Engineering", workspace.Name);
        Assert.Equal(42, workspace.CreatedByUserId);
        var member = Assert.Single(workspace.Members);
        Assert.Equal(42, member.UserId);
        Assert.Equal(Role.Owner, member.Role);
    }

    [Fact]
    public async Task GetWorkspacesAsync_returns_empty_list_as_success()
    {
        var service = CreateService(new FakeWorkspaceRepository(), currentUserId: 42);

        var result = await service.GetWorkSpacesAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task UpdateWorkspaceAsync_returns_forbidden_when_current_user_is_not_owner()
    {
        var repository = new FakeWorkspaceRepository();
        repository.Workspaces.Add(new WorkSpace
        {
            Id = 7,
            Name = "Engineering",
            CreatedByUserId = 100,
            Members =
            {
                new WorkspaceMember { WorkspaceId = 7, UserId = 42, Role = Role.Member }
            }
        });
        var service = CreateService(repository, currentUserId: 42);

        var result = await service.UpdateWorkspaceAsync(
            7,
            new UpdateWorkspaceRequest("Product"),
            CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.Forbidden, result.Status);
        Assert.Equal("Engineering", repository.Workspaces.Single().Name);
    }

    private static WorkspaceService CreateService(FakeWorkspaceRepository repository, int currentUserId)
    {
        return new WorkspaceService(
            new FakeCurrentUserService(currentUserId),
            repository,
            new FakeUnitOfWork(),
            new CreateWorkspaceRequestValidator(),
            new UpdateWorkspaceRequestValidator());
    }

    private sealed class FakeCurrentUserService(int userId) : ICurrentUserService
    {
        public int? UserId { get; } = userId;
        public bool IsAuthenticated => true;
        public int GetRequiredUserId() => userId;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class FakeWorkspaceRepository : IWorkspaceRepository
    {
        public List<WorkSpace> Workspaces { get; } = [];

        public Task<List<WorkSpace>> GetWorkspacesByUserIdAsync(int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Workspaces
                    .Where(workspace => !workspace.IsArchived)
                    .Where(workspace => workspace.Members.Any(member => member.UserId == userId))
                    .ToList());
        }

        public Task<WorkSpace?> GetWorkspaceForMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Workspaces.FirstOrDefault(workspace =>
                    workspace.Id == workspaceId &&
                    workspace.Members.Any(member => member.UserId == userId)));
        }

        public Task<WorkSpace?> GetWorkspaceForOwnerAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Workspaces.FirstOrDefault(workspace =>
                    workspace.Id == workspaceId &&
                    workspace.Members.Any(member => member.UserId == userId && member.Role == Role.Owner)));
        }

        public Task<List<WorkSpace>> GetAllAsync() => Task.FromResult(Workspaces);

        public Task<List<WorkSpace>> GetAllPagedAsync(int pageNumber, int pageSize)
        {
            return Task.FromResult(Workspaces.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList());
        }

        public IQueryable<WorkSpace> Where(Expression<Func<WorkSpace, bool>> predicate)
        {
            return Workspaces.AsQueryable().Where(predicate);
        }

        public ValueTask<WorkSpace?> GetByIdAsync(int id)
        {
            return ValueTask.FromResult(Workspaces.FirstOrDefault(workspace => workspace.Id == id));
        }

        public ValueTask AddAsync(WorkSpace entity)
        {
            entity.Id = entity.Id == 0 ? Workspaces.Count + 1 : entity.Id;
            Workspaces.Add(entity);
            return ValueTask.CompletedTask;
        }

        public Task<bool> AnyAsync(Expression<Func<WorkSpace, bool>> predicate)
        {
            return Task.FromResult(Workspaces.AsQueryable().Any(predicate));
        }

        public void Update(WorkSpace entity)
        {
        }

        public void Delete(WorkSpace entity)
        {
            Workspaces.Remove(entity);
        }
    }
}
