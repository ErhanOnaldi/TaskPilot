using System.Net;
using AutoMapper;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Features.Labels.Dtos;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Labels;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Labels.Services;

public class LabelService(
    ILabelRepository labelRepository,
    IGenericRepository<TaskItem> taskRepository,
    ITaskLabelRepository taskLabelRepository,
    IUnitOfWork unitOfWork,
    IAccessControlService accessControlService,
    IMapper mapper) : ILabelService
{
    public async Task<ServiceResult<List<LabelResponse>>> GetLabelsAsync(int projectId, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeProjectAsync(
            projectId,
            ProjectAccessLevel.Read,
            requireActiveProject: false,
            cancellationToken);
        if (access.Failure is not null)
        {
            return ServiceResult<List<LabelResponse>>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        var labels = await labelRepository.GetLabelsByProjectIdAsync(projectId, cancellationToken);
        return ServiceResult<List<LabelResponse>>.Success(mapper.Map<List<LabelResponse>>(labels));
    }

    public async Task<ServiceResult<LabelResponse>> CreateLabelAsync(int projectId, CreateLabelRequest request, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeProjectAsync(
            projectId,
            ProjectAccessLevel.Manage,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult<LabelResponse>.Fail("Only workspace owner or project manager can manage labels.", HttpStatusCode.Forbidden)
                : ServiceResult<LabelResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        var name = request.Name.Trim();
        if (await labelRepository.ExistsByNameInProjectAsync(projectId, name, cancellationToken))
        {
            return ServiceResult<LabelResponse>.Fail("Label already exists.", HttpStatusCode.Conflict);
        }

        var label = new Label
        {
            ProjectId = projectId,
            Name = name,
            Color = string.IsNullOrWhiteSpace(request.Color) ? "#3B82F6" : request.Color.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await labelRepository.AddAsync(label);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<LabelResponse>.Success(mapper.Map<LabelResponse>(label), HttpStatusCode.Created);
    }

    public async Task<ServiceResult> AddLabelToTaskAsync(int taskId, int labelId, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);
        var label = await labelRepository.GetByIdAsync(labelId);
        if (label is null || label.ProjectId != task.ProjectId) return ServiceResult.Fail("Label not found.", HttpStatusCode.NotFound);

        var access = await accessControlService.AuthorizeProjectAsync(
            task.ProjectId,
            ProjectAccessLevel.Participant,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only project members can manage task labels.", HttpStatusCode.Forbidden)
                : access.Failure;
        }

        if (await taskLabelRepository.TaskHasLabelAsync(taskId, labelId, cancellationToken))
        {
            return ServiceResult.Fail("Task already has this label.", HttpStatusCode.Conflict);
        }

        await taskLabelRepository.AddAsync(new TaskLabel { TaskId = taskId, LabelId = labelId });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> RemoveLabelFromTaskAsync(int taskId, int labelId, CancellationToken cancellationToken)
    {
        var task = await taskRepository.GetByIdAsync(taskId);
        if (task is null) return ServiceResult.Fail("Task not found.", HttpStatusCode.NotFound);
        var access = await accessControlService.AuthorizeProjectAsync(
            task.ProjectId,
            ProjectAccessLevel.Participant,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only project members can manage task labels.", HttpStatusCode.Forbidden)
                : access.Failure;
        }

        var taskLabel = await taskLabelRepository.GetTaskLabelAsync(taskId, labelId, cancellationToken);
        if (taskLabel is null) return ServiceResult.Fail("Task label not found.", HttpStatusCode.NotFound);

        taskLabelRepository.Delete(taskLabel);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }
}
