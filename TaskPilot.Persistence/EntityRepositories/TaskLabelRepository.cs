using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Interfaces.Persistence.Labels;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.EntityRepositories;

public sealed class TaskLabelRepository : GenericRepository<TaskLabel>, ITaskLabelRepository
{
    private readonly AppDbContext _dbContext;

    public TaskLabelRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> TaskHasLabelAsync(int taskId, int labelId, CancellationToken cancellationToken)
    {
        return _dbContext.TaskLabels
            .AsNoTracking()
            .AnyAsync(taskLabel => taskLabel.TaskId == taskId && taskLabel.LabelId == labelId, cancellationToken);
    }

    public Task<TaskLabel?> GetTaskLabelAsync(int taskId, int labelId, CancellationToken cancellationToken)
    {
        return _dbContext.TaskLabels
            .FirstOrDefaultAsync(taskLabel => taskLabel.TaskId == taskId && taskLabel.LabelId == labelId, cancellationToken);
    }
}
