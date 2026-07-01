using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Interfaces.Persistence.Labels;

public interface ITaskLabelRepository : IGenericRepository<TaskLabel>
{
    Task<bool> TaskHasLabelAsync(
        int taskId,
        int labelId,
        CancellationToken cancellationToken);

    Task<TaskLabel?> GetTaskLabelAsync(
        int taskId,
        int labelId,
        CancellationToken cancellationToken);
}
