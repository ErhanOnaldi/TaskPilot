using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;
using TaskPilot.Infrastructure.Authorization.Handlers;
using TaskPilot.Infrastructure.Authorization.Services;
using ProjectEntity = TaskPilot.Domain.Entities.Project;

namespace TaskPilot.Application.Tests;

public sealed class AccessControlServiceTests
{
    [Fact]
    public async Task AuthorizeProjectAsync_returns_not_found_when_user_is_not_workspace_member()
    {
        var projectRepository = new FakeProjectRepository();
        projectRepository.Projects.Add(new ProjectEntity { Id = 20, WorkspaceId = 10, Name = "API", Status = ProjectStatus.Active });
        var workspaceRepository = new FakeWorkspaceRepository();
        workspaceRepository.Workspaces.Add(new WorkSpace { Id = 10, Name = "Engineering" });
        var service = CreateService(projectRepository, workspaceRepository, new FakeWorkspaceMemberRepository(), 1);

        var result = await service.AuthorizeProjectAsync(
            20,
            ProjectAccessLevel.Read,
            requireActiveProject: false,
            CancellationToken.None);

        Assert.NotNull(result.Failure);
        Assert.Equal(HttpStatusCode.NotFound, result.Failure.Status);
    }

    [Fact]
    public async Task AuthorizeProjectAsync_allows_workspace_owner_to_manage_project()
    {
        var projectRepository = new FakeProjectRepository();
        projectRepository.Projects.Add(new ProjectEntity { Id = 20, WorkspaceId = 10, Name = "API", Status = ProjectStatus.Active });
        var workspaceRepository = new FakeWorkspaceRepository();
        workspaceRepository.Workspaces.Add(new WorkSpace { Id = 10, Name = "Engineering" });
        var workspaceMemberRepository = new FakeWorkspaceMemberRepository();
        workspaceMemberRepository.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 1, Role = Role.Owner });
        var service = CreateService(projectRepository, workspaceRepository, workspaceMemberRepository, 1);

        var result = await service.AuthorizeProjectAsync(
            20,
            ProjectAccessLevel.Manage,
            requireActiveProject: true,
            CancellationToken.None);

        Assert.Null(result.Failure);
        Assert.Equal(20, result.Project.Id);
        Assert.Equal(Role.Owner, result.WorkspaceMember.Role);
    }

    [Fact]
    public async Task AuthorizeProjectAsync_allows_project_manager_to_manage_project()
    {
        var projectRepository = new FakeProjectRepository();
        var project = new ProjectEntity { Id = 20, WorkspaceId = 10, Name = "API", Status = ProjectStatus.Active };
        project.Members.Add(new ProjectMember { ProjectId = 20, UserId = 2, Role = ProjectRole.ProjectManager });
        projectRepository.Projects.Add(project);
        var workspaceRepository = new FakeWorkspaceRepository();
        workspaceRepository.Workspaces.Add(new WorkSpace { Id = 10, Name = "Engineering" });
        var workspaceMemberRepository = new FakeWorkspaceMemberRepository();
        workspaceMemberRepository.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 2, Role = Role.Member });
        var service = CreateService(projectRepository, workspaceRepository, workspaceMemberRepository, 2);

        var result = await service.AuthorizeProjectAsync(
            20,
            ProjectAccessLevel.Manage,
            requireActiveProject: true,
            CancellationToken.None);

        Assert.Null(result.Failure);
        Assert.Equal(20, result.Project.Id);
    }

    [Fact]
    public async Task AuthorizeProjectAsync_rejects_project_member_for_manager_access()
    {
        var projectRepository = new FakeProjectRepository();
        var project = new ProjectEntity { Id = 20, WorkspaceId = 10, Name = "API", Status = ProjectStatus.Active };
        project.Members.Add(new ProjectMember { ProjectId = 20, UserId = 2, Role = ProjectRole.TeamMember });
        projectRepository.Projects.Add(project);
        var workspaceRepository = new FakeWorkspaceRepository();
        workspaceRepository.Workspaces.Add(new WorkSpace { Id = 10, Name = "Engineering" });
        var workspaceMemberRepository = new FakeWorkspaceMemberRepository();
        workspaceMemberRepository.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 2, Role = Role.Member });
        var service = CreateService(projectRepository, workspaceRepository, workspaceMemberRepository, 2);

        var result = await service.AuthorizeProjectAsync(
            20,
            ProjectAccessLevel.Manage,
            requireActiveProject: true,
            CancellationToken.None);

        Assert.NotNull(result.Failure);
        Assert.Equal(HttpStatusCode.Forbidden, result.Failure.Status);
    }

    [Fact]
    public async Task AuthorizeWorkspaceAsync_rejects_non_owner_for_owner_access()
    {
        var workspaceRepository = new FakeWorkspaceRepository();
        workspaceRepository.Workspaces.Add(new WorkSpace { Id = 10, Name = "Engineering" });
        var workspaceMemberRepository = new FakeWorkspaceMemberRepository();
        workspaceMemberRepository.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 2, Role = Role.Member });
        var service = CreateService(new FakeProjectRepository(), workspaceRepository, workspaceMemberRepository, 2);

        var result = await service.AuthorizeWorkspaceAsync(
            10,
            WorkspaceAccessLevel.Owner,
            requireActiveWorkspace: true,
            CancellationToken.None);

        Assert.NotNull(result.Failure);
        Assert.Equal(HttpStatusCode.Forbidden, result.Failure.Status);
    }

    private static AccessControlService CreateService(
        FakeProjectRepository projectRepository,
        FakeWorkspaceRepository workspaceRepository,
        FakeWorkspaceMemberRepository workspaceMemberRepository,
        int currentUserId)
    {
        return new AccessControlService(
            CreateAuthorizationService(),
            CreateHttpContextAccessor(currentUserId),
            new FakeCurrentUserService(currentUserId),
            workspaceRepository,
            workspaceMemberRepository,
            projectRepository,
            new FakeProjectMemberRepository(projectRepository));
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

    private sealed class FakeCurrentUserService(int userId) : ICurrentUserService
    {
        public int? UserId { get; } = userId;
        public bool IsAuthenticated => true;
        public int GetRequiredUserId() => userId;
    }

    private sealed class FakeProjectRepository : IProjectRepository
    {
        public List<ProjectEntity> Projects { get; } = [];

        public Task<List<ProjectEntity>> GetProjectsByWorkspaceIdAsync(int workspaceId, CancellationToken cancellationToken)
            => Task.FromResult(Projects.Where(project => project.WorkspaceId == workspaceId).ToList());

        public Task<ProjectEntity?> GetProjectByIdAsync(int projectId, CancellationToken cancellationToken)
            => Task.FromResult(Projects.FirstOrDefault(project => project.Id == projectId));

        public Task<ProjectEntity?> GetProjectForUpdateAsync(int projectId, CancellationToken cancellationToken)
            => GetProjectByIdAsync(projectId, cancellationToken);

        public Task<bool> ExistsByNameInWorkspaceAsync(int workspaceId, string name, CancellationToken cancellationToken)
            => Task.FromResult(Projects.Any(project => project.WorkspaceId == workspaceId && project.Name == name));

        public Task<bool> ExistsByNameInWorkspaceExceptProjectAsync(int workspaceId, int projectId, string name, CancellationToken cancellationToken)
            => Task.FromResult(Projects.Any(project => project.WorkspaceId == workspaceId && project.Id != projectId && project.Name == name));

        public Task<List<ProjectEntity>> GetAllAsync() => Task.FromResult(Projects);
        public Task<List<ProjectEntity>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(Projects);
        public IQueryable<ProjectEntity> Where(Expression<Func<ProjectEntity, bool>> predicate) => Projects.AsQueryable().Where(predicate);
        public ValueTask<ProjectEntity?> GetByIdAsync(int id) => ValueTask.FromResult(Projects.FirstOrDefault(project => project.Id == id));
        public ValueTask AddAsync(ProjectEntity entity) { Projects.Add(entity); return ValueTask.CompletedTask; }
        public Task<bool> AnyAsync(Expression<Func<ProjectEntity, bool>> predicate) => Task.FromResult(Projects.AsQueryable().Any(predicate));
        public void Update(ProjectEntity entity) { }
        public void Delete(ProjectEntity entity) => Projects.Remove(entity);
    }

    private sealed class FakeProjectMemberRepository(FakeProjectRepository projectRepository) : IProjectMemberRepository
    {
        public Task<List<ProjectMember>> GetMembersByProjectIdAsync(int projectId, CancellationToken cancellationToken)
            => Task.FromResult(projectRepository.Projects.Where(project => project.Id == projectId).SelectMany(project => project.Members).ToList());

        public Task<ProjectMember?> GetMemberAsync(int projectId, int userId, CancellationToken cancellationToken)
            => Task.FromResult(projectRepository.Projects.Where(project => project.Id == projectId).SelectMany(project => project.Members).FirstOrDefault(member => member.UserId == userId));

        public Task<bool> IsProjectMemberAsync(int projectId, int userId, CancellationToken cancellationToken)
            => Task.FromResult(projectRepository.Projects.Any(project => project.Id == projectId && project.Members.Any(member => member.UserId == userId)));

        public Task<bool> IsProjectManagerAsync(int projectId, int userId, CancellationToken cancellationToken)
            => Task.FromResult(projectRepository.Projects.Any(project => project.Id == projectId && project.Members.Any(member => member.UserId == userId && member.Role == ProjectRole.ProjectManager)));

        public Task<int> CountProjectManagersAsync(int projectId, CancellationToken cancellationToken)
            => Task.FromResult(projectRepository.Projects.Where(project => project.Id == projectId).SelectMany(project => project.Members).Count(member => member.Role == ProjectRole.ProjectManager));

        public Task<List<ProjectMember>> GetAllAsync() => Task.FromResult(projectRepository.Projects.SelectMany(project => project.Members).ToList());
        public Task<List<ProjectMember>> GetAllPagedAsync(int pageNumber, int pageSize) => GetAllAsync();
        public IQueryable<ProjectMember> Where(Expression<Func<ProjectMember, bool>> predicate) => projectRepository.Projects.SelectMany(project => project.Members).AsQueryable().Where(predicate);
        public ValueTask<ProjectMember?> GetByIdAsync(int id) => ValueTask.FromResult(projectRepository.Projects.SelectMany(project => project.Members).FirstOrDefault(member => member.Id == id));
        public ValueTask AddAsync(ProjectMember entity) { return ValueTask.CompletedTask; }
        public Task<bool> AnyAsync(Expression<Func<ProjectMember, bool>> predicate) => Task.FromResult(Where(predicate).Any());
        public void Update(ProjectMember entity) { }
        public void Delete(ProjectMember entity) { }
    }

    private sealed class FakeWorkspaceRepository : IWorkspaceRepository
    {
        public List<WorkSpace> Workspaces { get; } = [];

        public Task<List<WorkSpace>> GetWorkspacesByUserIdAsync(int userId, CancellationToken cancellationToken) => Task.FromResult(Workspaces);
        public Task<WorkSpace?> GetWorkspaceForMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken) => GetByIdAsync(workspaceId).AsTask();
        public Task<WorkSpace?> GetWorkspaceForOwnerAsync(int workspaceId, int userId, CancellationToken cancellationToken) => GetByIdAsync(workspaceId).AsTask();
        public Task<List<WorkSpace>> GetAllAsync() => Task.FromResult(Workspaces);
        public Task<List<WorkSpace>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(Workspaces);
        public IQueryable<WorkSpace> Where(Expression<Func<WorkSpace, bool>> predicate) => Workspaces.AsQueryable().Where(predicate);
        public ValueTask<WorkSpace?> GetByIdAsync(int id) => ValueTask.FromResult(Workspaces.FirstOrDefault(workspace => workspace.Id == id));
        public ValueTask AddAsync(WorkSpace entity) { Workspaces.Add(entity); return ValueTask.CompletedTask; }
        public Task<bool> AnyAsync(Expression<Func<WorkSpace, bool>> predicate) => Task.FromResult(Workspaces.AsQueryable().Any(predicate));
        public void Update(WorkSpace entity) { }
        public void Delete(WorkSpace entity) => Workspaces.Remove(entity);
    }

    private sealed class FakeWorkspaceMemberRepository : IWorkspaceMemberRepository
    {
        public List<WorkspaceMember> Members { get; } = [];

        public Task<List<WorkspaceMember>> GetMembersByWorkspaceIdAsync(int workspaceId, CancellationToken cancellationToken)
            => Task.FromResult(Members.Where(member => member.WorkspaceId == workspaceId).ToList());

        public Task<WorkspaceMember?> GetMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
            => Task.FromResult(Members.FirstOrDefault(member => member.WorkspaceId == workspaceId && member.UserId == userId));

        public Task<bool> IsWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
            => Task.FromResult(Members.Any(member => member.WorkspaceId == workspaceId && member.UserId == userId));

        public Task<bool> IsWorkspaceOwnerAsync(int workspaceId, int userId, CancellationToken cancellationToken)
            => Task.FromResult(Members.Any(member => member.WorkspaceId == workspaceId && member.UserId == userId && member.Role == Role.Owner));

        public Task<int> CountOwnersAsync(int workspaceId, CancellationToken cancellationToken)
            => Task.FromResult(Members.Count(member => member.WorkspaceId == workspaceId && member.Role == Role.Owner));

        public Task<List<WorkspaceMember>> GetAllAsync() => Task.FromResult(Members);
        public Task<List<WorkspaceMember>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(Members);
        public IQueryable<WorkspaceMember> Where(Expression<Func<WorkspaceMember, bool>> predicate) => Members.AsQueryable().Where(predicate);
        public ValueTask<WorkspaceMember?> GetByIdAsync(int id) => ValueTask.FromResult(Members.FirstOrDefault(member => member.Id == id));
        public ValueTask AddAsync(WorkspaceMember entity) { Members.Add(entity); return ValueTask.CompletedTask; }
        public Task<bool> AnyAsync(Expression<Func<WorkspaceMember, bool>> predicate) => Task.FromResult(Members.AsQueryable().Any(predicate));
        public void Update(WorkspaceMember entity) { }
        public void Delete(WorkspaceMember entity) => Members.Remove(entity);
    }
}
