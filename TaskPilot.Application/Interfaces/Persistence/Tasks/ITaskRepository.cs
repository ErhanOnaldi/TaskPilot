using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Interfaces.Persistence.Tasks;

public interface ITaskRepository : IGenericRepository<TaskItem>
{
    Task<PagedResponse<TaskItem>> GetTasksByProjectIdAsync(
        int projectId,
        TaskQueryParameters query,
        CancellationToken cancellationToken);
}
