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
using TaskPilot.Application.Features.Semantic.Contracts;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Application.Features.Tasks.Services;
using TaskPilot.Application.Features.Workspace.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
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
        projectRepository.Projects.Add(new ProjectEntity
        {
            Id = 20,
            WorkspaceId = 10,
            Name = "API",
            Status = ProjectStatus.Active,
            Members = [new ProjectMember { ProjectId = 20, UserId = 2, Role = ProjectRole.TeamMember }]
        });
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

    [Fact]
    public async Task UpdateStatusAsync_allows_team_member_to_update_an_assigned_task_through_valid_transition()
    {
        var taskRepository = new FakeTaskRepository();
        var now = new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);
        taskRepository.Tasks.Add(new TaskItem { Id = 1, ProjectId = 20, Title = "Auth API", Status = TaskItemStatus.Todo, AssignedUserId = 2 });
        var projectRepository = CreateProjectRepository(new ProjectMember { ProjectId = 20, UserId = 2, Role = ProjectRole.TeamMember });
        var workspaceRepository = CreateWorkspaceRepository();
        var service = CreateService(taskRepository, projectRepository, workspaceRepository, new FakeWorkspaceMemberRepository { Members = { new WorkspaceMember { WorkspaceId = 10, UserId = 2, Role = Role.Member } } }, 2);

        var result = await service.UpdateStatusAsync(1, new UpdateTaskStatusRequest(TaskItemStatus.InProgress), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TaskItemStatus.InProgress, taskRepository.Tasks.Single().Status);
        Assert.Equal(now, taskRepository.Tasks.Single().UpdatedAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_rejects_team_member_updating_another_users_task()
    {
        var taskRepository = new FakeTaskRepository();
        taskRepository.Tasks.Add(new TaskItem { Id = 1, ProjectId = 20, Title = "Auth API", Status = TaskItemStatus.Todo, AssignedUserId = 3 });
        var projectRepository = CreateProjectRepository(new ProjectMember { ProjectId = 20, UserId = 2, Role = ProjectRole.TeamMember });
        var workspaceRepository = CreateWorkspaceRepository();
        var workspaceMembers = new FakeWorkspaceMemberRepository();
        workspaceMembers.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 2, Role = Role.Member });
        var service = CreateService(taskRepository, projectRepository, workspaceRepository, workspaceMembers, 2);

        var result = await service.UpdateStatusAsync(1, new UpdateTaskStatusRequest(TaskItemStatus.InProgress), CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_rejects_team_member_reopening_cancelled_task()
    {
        var taskRepository = new FakeTaskRepository();
        taskRepository.Tasks.Add(new TaskItem { Id = 1, ProjectId = 20, Title = "Auth API", Status = TaskItemStatus.Cancelled, AssignedUserId = 2 });
        var projectRepository = CreateProjectRepository(new ProjectMember { ProjectId = 20, UserId = 2, Role = ProjectRole.TeamMember });
        var workspaceRepository = CreateWorkspaceRepository();
        var workspaceMembers = new FakeWorkspaceMemberRepository();
        workspaceMembers.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 2, Role = Role.Member });
        var service = CreateService(taskRepository, projectRepository, workspaceRepository, workspaceMembers, 2);

        var result = await service.UpdateStatusAsync(1, new UpdateTaskStatusRequest(TaskItemStatus.InProgress), CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.Status);
    }

    [Fact]
    public async Task CreateTaskAsync_rejects_guest_assignee()
    {
        var taskRepository = new FakeTaskRepository();
        var projectRepository = CreateProjectRepository(
            new ProjectMember { ProjectId = 20, UserId = 1, Role = ProjectRole.ProjectManager },
            new ProjectMember { ProjectId = 20, UserId = 2, Role = ProjectRole.Guest });
        var workspaceRepository = CreateWorkspaceRepository();
        var workspaceMembers = new FakeWorkspaceMemberRepository();
        workspaceMembers.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 1, Role = Role.Member });
        var service = CreateService(taskRepository, projectRepository, workspaceRepository, workspaceMembers, 1);

        var result = await service.CreateTaskAsync(20, new CreateTaskRequest("Auth API", null, null, null, 2), CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.Status);
        Assert.Contains(result.ErrorMessages!, message => message.Contains("Guest", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CreateTaskAsync_rejects_team_member_assigning_task_to_another_user()
    {
        var taskRepository = new FakeTaskRepository();
        var projectRepository = CreateProjectRepository(
            new ProjectMember { ProjectId = 20, UserId = 2, Role = ProjectRole.TeamMember },
            new ProjectMember { ProjectId = 20, UserId = 3, Role = ProjectRole.TeamMember });
        var workspaceRepository = CreateWorkspaceRepository();
        var workspaceMembers = new FakeWorkspaceMemberRepository();
        workspaceMembers.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 2, Role = Role.Member });
        workspaceMembers.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 3, Role = Role.Member });
        var service = CreateService(taskRepository, projectRepository, workspaceRepository, workspaceMembers, 2);

        var result = await service.CreateTaskAsync(
            20,
            new CreateTaskRequest("Auth API", null, null, null, 3),
            CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.Status);
        Assert.Empty(taskRepository.Tasks);
    }

    [Fact]
    public async Task CreateTaskAsync_allows_team_member_assigning_task_to_self()
    {
        var taskRepository = new FakeTaskRepository();
        var projectRepository = CreateProjectRepository(
            new ProjectMember { ProjectId = 20, UserId = 2, Role = ProjectRole.TeamMember });
        var workspaceRepository = CreateWorkspaceRepository();
        var workspaceMembers = new FakeWorkspaceMemberRepository();
        workspaceMembers.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 2, Role = Role.Member });
        var service = CreateService(taskRepository, projectRepository, workspaceRepository, workspaceMembers, 2);

        var result = await service.CreateTaskAsync(
            20,
            new CreateTaskRequest("Auth API", null, null, null, 2),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, Assert.Single(taskRepository.Tasks).AssignedUserId);
    }

    [Fact]
    public async Task CreateTaskAsync_returns_duplicate_warning_without_blocking_creation()
    {
        var taskRepository = new FakeTaskRepository();
        var projectRepository = CreateProjectRepository(new ProjectMember { ProjectId = 20, UserId = 1, Role = ProjectRole.ProjectManager });
        var workspaceRepository = CreateWorkspaceRepository();
        var workspaceMembers = new FakeWorkspaceMemberRepository();
        workspaceMembers.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 1, Role = Role.Member });
        var service = CreateService(
            taskRepository,
            projectRepository,
            workspaceRepository,
            workspaceMembers,
            1,
            new DuplicateDetector(new DuplicateTaskWarning([new SimilarTaskMatch(71, "Repair sign-in flow", .91f)])));

        var result = await service.CreateTaskAsync(20, new CreateTaskRequest("Fix login failure", null, null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(System.Net.HttpStatusCode.Created, result.Status);
        Assert.Single(taskRepository.Tasks);
        var match = Assert.Single(result.Data!.DuplicateWarning!.Matches);
        Assert.Equal(71, match.TaskId);
        Assert.Equal(.91f, match.Similarity);
    }

    [Fact]
    public async Task CreateTaskAsync_persists_task_when_duplicate_detector_fails()
    {
        var taskRepository = new FakeTaskRepository();
        var projectRepository = CreateProjectRepository(new ProjectMember { ProjectId = 20, UserId = 1, Role = ProjectRole.ProjectManager });
        var workspaceRepository = CreateWorkspaceRepository();
        var workspaceMembers = new FakeWorkspaceMemberRepository();
        workspaceMembers.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 1, Role = Role.Member });
        var service = CreateService(taskRepository, projectRepository, workspaceRepository, workspaceMembers, 1, new FailingDuplicateDetector());

        var result = await service.CreateTaskAsync(20, new CreateTaskRequest("Fix login failure", null, null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(taskRepository.Tasks);
        Assert.Null(result.Data!.DuplicateWarning);
    }

    [Fact]
    public async Task UpdateTaskAsync_rejects_team_member_editing_task_details()
    {
        var taskRepository = new FakeTaskRepository();
        taskRepository.Tasks.Add(new TaskItem
        {
            Id = 1,
            ProjectId = 20,
            Title = "Original",
            Status = TaskItemStatus.Todo,
            AssignedUserId = 2
        });
        var projectRepository = CreateProjectRepository(
            new ProjectMember { ProjectId = 20, UserId = 2, Role = ProjectRole.TeamMember });
        var workspaceRepository = CreateWorkspaceRepository();
        var workspaceMembers = new FakeWorkspaceMemberRepository();
        workspaceMembers.Members.Add(new WorkspaceMember { WorkspaceId = 10, UserId = 2, Role = Role.Member });
        var service = CreateService(taskRepository, projectRepository, workspaceRepository, workspaceMembers, 2);

        var result = await service.UpdateTaskAsync(
            1,
            new UpdateTaskRequest("Changed", null, null, TaskItemPriority.High, 2),
            CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.Status);
        Assert.Equal("Original", taskRepository.Tasks.Single().Title);
    }

    private static FakeProjectRepository CreateProjectRepository(params ProjectMember[] members)
    {
        var repository = new FakeProjectRepository();
        repository.Projects.Add(new ProjectEntity { Id = 20, WorkspaceId = 10, Name = "API", Status = ProjectStatus.Active, Members = members.ToList() });
        return repository;
    }

    private static FakeWorkspaceRepository CreateWorkspaceRepository()
    {
        var repository = new FakeWorkspaceRepository();
        repository.Workspaces.Add(new WorkSpace { Id = 10, Name = "Engineering" });
        return repository;
    }

    private static TaskService CreateService(
        FakeTaskRepository taskRepository,
        FakeProjectRepository projectRepository,
        FakeWorkspaceRepository workspaceRepository,
        FakeWorkspaceMemberRepository workspaceMemberRepository,
        int currentUserId,
        ITaskDuplicateDetector? duplicateDetector = null)
    {
        return new TaskService(
            taskRepository,
            new FakeProjectMemberRepository(projectRepository),
            workspaceMemberRepository,
            new FakeUnitOfWork(),
            new AccessControlService(
                CreateAuthorizationService(),
                CreateHttpContextAccessor(currentUserId),
                new FakeCurrentUserService(currentUserId),
                workspaceRepository,
                workspaceMemberRepository,
                projectRepository,
                new FakeProjectMemberRepository(projectRepository)),
            CreateMapper(),
            new FakeEventOutbox(),
            new FakeDateTimeProvider(),
            duplicateDetector);
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

    private sealed class FakeDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class DuplicateDetector(DuplicateTaskWarning? warning) : ITaskDuplicateDetector
    {
        public Task<DuplicateTaskWarning?> DetectAsync(int projectId, int taskId, string content, CancellationToken cancellationToken) => Task.FromResult(warning);
    }

    private sealed class FailingDuplicateDetector : ITaskDuplicateDetector
    {
        public Task<DuplicateTaskWarning?> DetectAsync(int projectId, int taskId, string content, CancellationToken cancellationToken) => throw new InvalidOperationException("Embedding provider is unavailable.");
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeEventOutbox : IEventOutbox
    {
        public Task EnqueueAsync(Func<IIntegrationEvent> eventFactory, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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

        public Task<PagedResponse<ProjectEntity>> GetProjectsByWorkspaceIdAsync(int workspaceId, int? projectMemberUserId, ProjectQueryParameters query, CancellationToken cancellationToken)
            => GetProjectsByWorkspaceIdAsync(workspaceId, query, cancellationToken);

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
