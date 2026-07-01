using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Authorization.Results;
using TaskPilot.Application.Features.Labels.Dtos;
using TaskPilot.Application.Features.Labels.Services;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Labels;
using TaskPilot.Application.Mappings;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Tests;

public class LabelServiceTests
{
    [Fact]
    public async Task CreateLabelAsync_returns_conflict_when_label_name_exists_in_project()
    {
        var labelRepository = new FakeLabelRepository { ExistsByName = true };
        var service = CreateService(labelRepository, new FakeTaskLabelRepository(), new FakeTaskRepository());

        var result = await service.CreateLabelAsync(20, new CreateLabelRequest("Bug", "#111111"), CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(System.Net.HttpStatusCode.Conflict, result.Status);
        Assert.Equal((20, "Bug"), labelRepository.LastExistsByNameRequest);
    }

    [Fact]
    public async Task RemoveLabelFromTaskAsync_returns_not_found_when_task_label_does_not_exist()
    {
        var taskRepository = new FakeTaskRepository();
        taskRepository.Tasks.Add(new TaskItem { Id = 10, ProjectId = 20, Title = "Task", CreatedByUserId = 1 });
        var service = CreateService(new FakeLabelRepository(), new FakeTaskLabelRepository(), taskRepository);

        var result = await service.RemoveLabelFromTaskAsync(10, 5, CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(System.Net.HttpStatusCode.NotFound, result.Status);
    }

    private static LabelService CreateService(
        FakeLabelRepository labelRepository,
        FakeTaskLabelRepository taskLabelRepository,
        FakeTaskRepository taskRepository)
    {
        return new LabelService(
            labelRepository,
            taskRepository,
            taskLabelRepository,
            new FakeUnitOfWork(),
            new FakeAccessControlService(),
            CreateMapper());
    }

    private static IMapper CreateMapper()
    {
        return new MapperConfiguration(
            configuration => configuration.AddProfile<ApplicationMappingProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
    }

    private sealed class FakeLabelRepository : ILabelRepository
    {
        public List<Label> Labels { get; } = [];
        public bool ExistsByName { get; init; }
        public (int ProjectId, string Name) LastExistsByNameRequest { get; private set; }

        public Task<List<Label>> GetLabelsByProjectIdAsync(int projectId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Labels.Where(label => label.ProjectId == projectId).OrderBy(label => label.Name).ToList());
        }

        public Task<bool> ExistsByNameInProjectAsync(int projectId, string name, CancellationToken cancellationToken)
        {
            LastExistsByNameRequest = (projectId, name);
            return Task.FromResult(ExistsByName);
        }

        public Task<List<Label>> GetAllAsync() => Task.FromResult(Labels);
        public Task<List<Label>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(Labels);
        public IQueryable<Label> Where(Expression<Func<Label, bool>> predicate) => Labels.AsQueryable().Where(predicate);
        public ValueTask<Label?> GetByIdAsync(int id) => ValueTask.FromResult(Labels.FirstOrDefault(label => label.Id == id));
        public ValueTask AddAsync(Label entity) { Labels.Add(entity); return ValueTask.CompletedTask; }
        public Task<bool> AnyAsync(Expression<Func<Label, bool>> predicate) => Task.FromResult(Labels.AsQueryable().Any(predicate));
        public void Update(Label entity) { }
        public void Delete(Label entity) => Labels.Remove(entity);
    }

    private sealed class FakeTaskLabelRepository : ITaskLabelRepository
    {
        public List<TaskLabel> TaskLabels { get; } = [];

        public Task<bool> TaskHasLabelAsync(int taskId, int labelId, CancellationToken cancellationToken)
        {
            return Task.FromResult(TaskLabels.Any(taskLabel => taskLabel.TaskId == taskId && taskLabel.LabelId == labelId));
        }

        public Task<TaskLabel?> GetTaskLabelAsync(int taskId, int labelId, CancellationToken cancellationToken)
        {
            return Task.FromResult(TaskLabels.FirstOrDefault(taskLabel => taskLabel.TaskId == taskId && taskLabel.LabelId == labelId));
        }

        public Task<List<TaskLabel>> GetAllAsync() => Task.FromResult(TaskLabels);
        public Task<List<TaskLabel>> GetAllPagedAsync(int pageNumber, int pageSize) => Task.FromResult(TaskLabels);
        public IQueryable<TaskLabel> Where(Expression<Func<TaskLabel, bool>> predicate) => TaskLabels.AsQueryable().Where(predicate);
        public ValueTask<TaskLabel?> GetByIdAsync(int id) => ValueTask.FromResult<TaskLabel?>(null);
        public ValueTask AddAsync(TaskLabel entity) { TaskLabels.Add(entity); return ValueTask.CompletedTask; }
        public Task<bool> AnyAsync(Expression<Func<TaskLabel, bool>> predicate) => Task.FromResult(TaskLabels.AsQueryable().Any(predicate));
        public void Update(TaskLabel entity) { }
        public void Delete(TaskLabel entity) => TaskLabels.Remove(entity);
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
