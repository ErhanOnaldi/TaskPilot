using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TaskPilot.Application;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Features.Workspace.Dtos;
using TaskPilot.Application.Features.Workspace.Services;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Application.Mappings;
using TaskPilot.Domain.Entities;
using TaskPilot.Infrastructure.Authorization.Handlers;
using TaskPilot.Infrastructure.Authorization.Services;
using ProjectEntity = TaskPilot.Domain.Entities.Project;

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
            new AccessControlService(
                CreateAuthorizationService(),
                CreateHttpContextAccessor(currentUserId),
                new FakeCurrentUserService(currentUserId),
                repository,
                new FakeWorkspaceMemberRepository(repository),
                new FakeProjectRepository(),
                new FakeProjectMemberRepository()),
            new FakeUnitOfWork(),
            CreateMapper());
    }

    private static IAuthorizationService CreateAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        services.AddScoped<IAuthorizationHandler, WorkspaceAccessHandler>();
        services.AddScoped<IAuthorizationHandler, ProjectAccessHandler>();
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(int currentUserId)
    {
        return new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString())],
                        "Test"))
            }
        };
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

    private sealed class FakeWorkspaceMemberRepository(FakeWorkspaceRepository workspaceRepository) : IWorkspaceMemberRepository
    {
        public Task<List<WorkspaceMember>> GetMembersByWorkspaceIdAsync(int workspaceId, CancellationToken cancellationToken)
        {
            return Task.FromResult(workspaceRepository.Workspaces
                .Where(workspace => workspace.Id == workspaceId)
                .SelectMany(workspace => workspace.Members)
                .ToList());
        }

        public Task<WorkspaceMember?> GetMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(workspaceRepository.Workspaces
                .Where(workspace => workspace.Id == workspaceId)
                .SelectMany(workspace => workspace.Members)
                .FirstOrDefault(member => member.UserId == userId));
        }

        public Task<bool> IsWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(workspaceRepository.Workspaces.Any(workspace =>
                workspace.Id == workspaceId &&
                workspace.Members.Any(member => member.UserId == userId)));
        }

        public Task<bool> IsWorkspaceOwnerAsync(int workspaceId, int userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(workspaceRepository.Workspaces.Any(workspace =>
                workspace.Id == workspaceId &&
                workspace.Members.Any(member => member.UserId == userId && member.Role == Role.Owner)));
        }

        public Task<int> CountOwnersAsync(int workspaceId, CancellationToken cancellationToken)
        {
            return Task.FromResult(workspaceRepository.Workspaces
                .Where(workspace => workspace.Id == workspaceId)
                .SelectMany(workspace => workspace.Members)
                .Count(member => member.Role == Role.Owner));
        }

        public Task<List<WorkspaceMember>> GetAllAsync()
        {
            return Task.FromResult(workspaceRepository.Workspaces.SelectMany(workspace => workspace.Members).ToList());
        }

        public Task<List<WorkspaceMember>> GetAllPagedAsync(int pageNumber, int pageSize)
        {
            return GetAllAsync();
        }

        public IQueryable<WorkspaceMember> Where(Expression<Func<WorkspaceMember, bool>> predicate)
        {
            return workspaceRepository.Workspaces.SelectMany(workspace => workspace.Members).AsQueryable().Where(predicate);
        }

        public ValueTask<WorkspaceMember?> GetByIdAsync(int id)
        {
            return ValueTask.FromResult(workspaceRepository.Workspaces.SelectMany(workspace => workspace.Members).FirstOrDefault(member => member.Id == id));
        }

        public ValueTask AddAsync(WorkspaceMember entity)
        {
            return ValueTask.CompletedTask;
        }

        public Task<bool> AnyAsync(Expression<Func<WorkspaceMember, bool>> predicate)
        {
            return Task.FromResult(Where(predicate).Any());
        }

        public void Update(WorkspaceMember entity)
        {
        }

        public void Delete(WorkspaceMember entity)
        {
        }
    }

    private sealed class FakeProjectRepository : IProjectRepository
    {
        public Task<List<ProjectEntity>> GetProjectsByWorkspaceIdAsync(int workspaceId, CancellationToken cancellationToken) => Task.FromResult(new List<ProjectEntity>());
        public Task<ProjectEntity?> GetProjectByIdAsync(int projectId, CancellationToken cancellationToken) => Task.FromResult<ProjectEntity?>(null);
        public Task<ProjectEntity?> GetProjectForUpdateAsync(int projectId, CancellationToken cancellationToken) => Task.FromResult<ProjectEntity?>(null);
        public Task<bool> ExistsByNameInWorkspaceAsync(int workspaceId, string name, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> ExistsByNameInWorkspaceExceptProjectAsync(int workspaceId, int projectId, string name, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<List<ProjectEntity>> GetAllAsync() => Task.FromResult(new List<ProjectEntity>());
        public Task<List<ProjectEntity>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(new List<ProjectEntity>());
        public IQueryable<ProjectEntity> Where(Expression<Func<ProjectEntity, bool>> predicate) => new List<ProjectEntity>().AsQueryable().Where(predicate);
        public ValueTask<ProjectEntity?> GetByIdAsync(int id) => ValueTask.FromResult<ProjectEntity?>(null);
        public ValueTask AddAsync(ProjectEntity entity) => ValueTask.CompletedTask;
        public Task<bool> AnyAsync(Expression<Func<ProjectEntity, bool>> predicate) => Task.FromResult(false);
        public void Update(ProjectEntity entity) { }
        public void Delete(ProjectEntity entity) { }
    }

    private sealed class FakeProjectMemberRepository : IProjectMemberRepository
    {
        public Task<List<ProjectMember>> GetMembersByProjectIdAsync(int projectId, CancellationToken cancellationToken) => Task.FromResult(new List<ProjectMember>());
        public Task<ProjectMember?> GetMemberAsync(int projectId, int userId, CancellationToken cancellationToken) => Task.FromResult<ProjectMember?>(null);
        public Task<bool> IsProjectMemberAsync(int projectId, int userId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> IsProjectManagerAsync(int projectId, int userId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<int> CountProjectManagersAsync(int projectId, CancellationToken cancellationToken) => Task.FromResult(0);
        public Task<List<ProjectMember>> GetAllAsync() => Task.FromResult(new List<ProjectMember>());
        public Task<List<ProjectMember>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(new List<ProjectMember>());
        public IQueryable<ProjectMember> Where(Expression<Func<ProjectMember, bool>> predicate) => new List<ProjectMember>().AsQueryable().Where(predicate);
        public ValueTask<ProjectMember?> GetByIdAsync(int id) => ValueTask.FromResult<ProjectMember?>(null);
        public ValueTask AddAsync(ProjectMember entity) => ValueTask.CompletedTask;
        public Task<bool> AnyAsync(Expression<Func<ProjectMember, bool>> predicate) => Task.FromResult(false);
        public void Update(ProjectMember entity) { }
        public void Delete(ProjectMember entity) { }
    }
}
