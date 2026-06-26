using TaskPilot.Application.Features.Labels.Dtos;

namespace TaskPilot.Application.Features.Labels.Services;

public interface ILabelService
{
    Task<ServiceResult<List<LabelResponse>>> GetLabelsAsync(int projectId, CancellationToken cancellationToken);
    Task<ServiceResult<LabelResponse>> CreateLabelAsync(int projectId, CreateLabelRequest request, CancellationToken cancellationToken);
    Task<ServiceResult> AddLabelToTaskAsync(int taskId, int labelId, CancellationToken cancellationToken);
    Task<ServiceResult> RemoveLabelFromTaskAsync(int taskId, int labelId, CancellationToken cancellationToken);
}
