using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Tasks.Dtos;

namespace TaskPilot.Application.Features.Tasks.Services;

public interface ITaskService
{
    Task<ServiceResult<PagedResponse<TaskResponse>>> GetTasksAsync(
        int projectId,
        TaskQueryParameters query,
        CancellationToken cancellationToken);
    Task<ServiceResult<TaskResponse>> GetTaskAsync(int taskId, CancellationToken cancellationToken);
    Task<ServiceResult<TaskResponse>> CreateTaskAsync(int projectId, CreateTaskRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> UpdateTaskAsync(int taskId, UpdateTaskRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> DeleteTaskAsync(int taskId, CancellationToken cancellationToken);
    Task<ServiceResult> UpdateStatusAsync(int taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> AssignTaskAsync(int taskId, AssignTaskRequest request, CancellationToken cancellationToken);
}
