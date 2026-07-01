using System.Linq.Expressions;
using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Project.Dtos;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Application.Features.Tasks.Services;
using TaskPilot.Application.Features.Workspace.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Tasks;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Application.Mappings;
using TaskPilot.Domain.Entities;
using TaskPilot.Infrastructure.Authorization.Handlers;
using TaskPilot.Infrastructure.Authorization.Services;
using ProjectEntity = TaskPilot.Domain.Entities.Project;

namespace TaskPilot.Application.Tests;

public class TaskServiceTests
{
    [Fact]
    public async Task GetTasksAsync_returns_paged_tasks_for_authorized_workspace_member()
    {
        var taskRepository = new FakeTaskRepository();
        taskRepository.Tasks.AddRange(
        [
            new TaskItem { Id = 1, ProjectId = 20, Title = "Auth API", Status = TaskItemStatus.Todo, Priority = TaskItemPriority.High, AssignedUserId = 2, CreatedByUserId = 1, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new TaskItem { Id = 2, ProjectId = 20, Title = "Billing UI", Status = TaskItemStatus.Done, Priority = TaskItemPriority.Low, AssignedUserId = 2, CreatedByUserId = 1, CreatedAt = DateTime.UtcNow.AddDays(-1) },
            new TaskItem { Id = 3, ProjectId = 20, Title = "Auth refresh", Status = TaskItemStatus.Todo, Priority = TaskItemPriority.High, AssignedUserId = 3, CreatedByUserId = 1, CreatedAt = DateTime.UtcNow }
        ]);

        var projectRepository = new FakeProjectRepository();
        projectRepository.Projects.Add(new ProjectEntity { Id = 20, WorkspaceId = 10, Name = "API", Status = ProjectStatus.Active });
        var workspaceRepository = new FakeWorkspaceRepository();
        workspaceRepository.Workspaces.Add(new WorkSpace { Id = 10, Name = "Engineering" });
        var workspaceMemberRepository = new FakeWorkspaceMemberRepository();
        workspaceMemberRepository.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 2, Role = Role.Member });
        var service = CreateService(taskRepository, projectRepository, workspaceRepository, workspaceMemberRepository, 2);

        var result = await service.GetTasksAsync(
            20,
            new TaskQueryParameters
            {
                PageNumber = 1,
                PageSize = 1,
                Status = TaskItemStatus.Todo,
                Priority = TaskItemPriority.High,
                Search = "auth",
                SortBy = TaskSortBy.CreatedAt,
                SortDirection = SortDirection.Desc
            },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.TotalCount);
        Assert.Equal(2, result.Data.TotalPages);
        Assert.True(result.Data.HasNextPage);
        var task = Assert.Single(result.Data.Items);
        Assert.Equal(3, task.Id);
    }

    private static TaskService CreateService(
        FakeTaskRepository taskRepository,
        FakeProjectRepository projectRepository,
        FakeWorkspaceRepository workspaceRepository,
        FakeWorkspaceMemberRepository workspaceMemberRepository,
        int currentUserId)
    {
        return new TaskService(
            taskRepository,
            new FakeProjectMemberRepository(projectRepository),
            new FakeUnitOfWork(),
            new AccessControlService(
                CreateAuthorizationService(),
                CreateHttpContextAccessor(currentUserId),
                new FakeCurrentUserService(currentUserId),
                workspaceRepository,
                workspaceMemberRepository,
                projectRepository,
                new FakeProjectMemberRepository(projectRepository)),
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
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeTaskRepository : ITaskRepository
    {
        public List<TaskItem> Tasks { get; } = [];

        public Task<PagedResponse<TaskItem>> GetTasksByProjectIdAsync(int projectId, TaskQueryParameters query, CancellationToken cancellationToken)
        {
            var tasks = Tasks
                .Where(task => task.ProjectId == projectId)
                .Where(task => !query.Status.HasValue || task.Status == query.Status.Value)
                .Where(task => !query.Priority.HasValue || task.Priority == query.Priority.Value)
                .Where(task => !query.AssignedUserId.HasValue || task.AssignedUserId == query.AssignedUserId.Value);

            var search = query.Search?.Trim();
            if (!string.IsNullOrWhiteSpace(search))
            {
                tasks = tasks.Where(task =>
                    task.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (task.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            tasks = query.SortDirection == SortDirection.Asc
                ? tasks.OrderBy(task => task.CreatedAt).ThenByDescending(task => task.Id)
                : tasks.OrderByDescending(task => task.CreatedAt).ThenByDescending(task => task.Id);

            var filteredTasks = tasks.ToList();
            var pageItems = filteredTasks
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return Task.FromResult(PagedResponse<TaskItem>.Create(pageItems, query.PageNumber, query.PageSize, filteredTasks.Count));
        }

        public Task<List<TaskItem>> GetAllAsync() => Task.FromResult(Tasks);
        public Task<List<TaskItem>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(Tasks.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList());
        public IQueryable<TaskItem> Where(Expression<Func<TaskItem, bool>> predicate) => Tasks.AsQueryable().Where(predicate);
        public ValueTask<TaskItem?> GetByIdAsync(int id) => ValueTask.FromResult(Tasks.FirstOrDefault(task => task.Id == id));
        public ValueTask AddAsync(TaskItem entity) { Tasks.Add(entity); return ValueTask.CompletedTask; }
        public Task<bool> AnyAsync(Expression<Func<TaskItem, bool>> predicate) => Task.FromResult(Tasks.AsQueryable().Any(predicate));
        public void Update(TaskItem entity) { }
        public void Delete(TaskItem entity) => Tasks.Remove(entity);
    }

    private sealed class FakeProjectRepository : IProjectRepository
    {
        public List<ProjectEntity> Projects { get; } = [];

        public Task<List<ProjectEntity>> GetProjectsByWorkspaceIdAsync(int workspaceId, CancellationToken cancellationToken)
            => Task.FromResult(Projects.Where(project => project.WorkspaceId == workspaceId).ToList());

        public Task<PagedResponse<ProjectEntity>> GetProjectsByWorkspaceIdAsync(int workspaceId, ProjectQueryParameters query, CancellationToken cancellationToken)
        {
            var projects = Projects.Where(project => project.WorkspaceId == workspaceId).ToList();
            return Task.FromResult(PagedResponse<ProjectEntity>.Create(projects, query.PageNumber, query.PageSize, projects.Count));
        }

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
        public ValueTask AddAsync(ProjectMember entity) => ValueTask.CompletedTask;
        public Task<bool> AnyAsync(Expression<Func<ProjectMember, bool>> predicate) => Task.FromResult(Where(predicate).Any());
        public void Update(ProjectMember entity) { }
        public void Delete(ProjectMember entity) { }
    }

    private sealed class FakeWorkspaceRepository : IWorkspaceRepository
    {
        public List<WorkSpace> Workspaces { get; } = [];

        public Task<List<WorkSpace>> GetWorkspacesByUserIdAsync(int userId, CancellationToken cancellationToken) => Task.FromResult(Workspaces);
        public Task<PagedResponse<WorkSpace>> GetWorkspacesByUserIdAsync(int userId, WorkspaceQueryParameters query, CancellationToken cancellationToken)
            => Task.FromResult(PagedResponse<WorkSpace>.Create(Workspaces, query.PageNumber, query.PageSize, Workspaces.Count));
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

        public Task<List<WorkspaceMember>> GetMembersByWorkspaceIdAsync(int workspaceId, CancellationToken cancellationToken) => Task.FromResult(Members.Where(member => member.WorkspaceId == workspaceId).ToList());
        public Task<WorkspaceMember?> GetMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken) => Task.FromResult(Members.FirstOrDefault(member => member.WorkspaceId == workspaceId && member.UserId == userId));
        public Task<bool> IsWorkspaceMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken) => Task.FromResult(Members.Any(member => member.WorkspaceId == workspaceId && member.UserId == userId));
        public Task<bool> IsWorkspaceOwnerAsync(int workspaceId, int userId, CancellationToken cancellationToken) => Task.FromResult(Members.Any(member => member.WorkspaceId == workspaceId && member.UserId == userId && member.Role == Role.Owner));
        public Task<int> CountOwnersAsync(int workspaceId, CancellationToken cancellationToken) => Task.FromResult(Members.Count(member => member.WorkspaceId == workspaceId && member.Role == Role.Owner));
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
