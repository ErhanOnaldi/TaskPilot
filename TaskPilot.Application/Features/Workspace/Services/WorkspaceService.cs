using System.Net;
using FluentValidation;
using TaskPilot.Application.Features.Workspace.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Workspace.Services;

public class WorkspaceService(
    ICurrentUserService currentUserService,
    IWorkspaceRepository workspaceRepository,
    IUnitOfWork unitOfWork,
    IValidator<CreateWorkspaceRequest> createValidator,
    IValidator<UpdateWorkspaceRequest> updateValidator) : IWorkspaceService
{
    public async Task<ServiceResult<WorkspaceResponse>> CreateWorkspaceAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ServiceResult<WorkspaceResponse>.Fail(
                validationResult.Errors.Select(x => x.ErrorMessage).ToList(),
                HttpStatusCode.BadRequest);
        }

        var userId = currentUserService.UserId;
        var workspace = new WorkSpace()
        {
            Name = request.Name.Trim(),
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
        };
        workspace.Members.Add(new WorkspaceMember
        {
            UserId = userId,
            Role = Role.Owner,
            JoinedAt = DateTime.UtcNow
        });

        await workspaceRepository.AddAsync(workspace);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<WorkspaceResponse>.SuccessAsCreated(
            MapToResponse(workspace),
            $"api/workspaces/{workspace.Id}");
    }

    public async Task<ServiceResult<List<WorkspaceListItemResponse>>> GetWorkSpacesAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;
        var workspaces = await workspaceRepository.GetWorkspacesByUserIdAsync(userId, cancellationToken);

        return ServiceResult<List<WorkspaceListItemResponse>>.Success(
            workspaces.Select(MapToListItemResponse).ToList());
    }

    public async Task<ServiceResult<WorkspaceResponse>> GetWorkspaceAsync(int id, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.GetWorkspaceForMemberAsync(id, currentUserService.UserId, cancellationToken);
        if (workspace == null)
        {
            return ServiceResult<WorkspaceResponse>.Fail("Workspace not found.", HttpStatusCode.NotFound);
        }

        return ServiceResult<WorkspaceResponse>.Success(MapToResponse(workspace));
    }

    public async Task<ServiceResult> UpdateWorkspaceAsync(int id, UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ServiceResult.Fail(
                validationResult.Errors.Select(x => x.ErrorMessage).ToList(),
                HttpStatusCode.BadRequest);
        }

        var currentUserId = currentUserService.UserId;
        var memberWorkspace = await workspaceRepository.GetWorkspaceForMemberAsync(id, currentUserId, cancellationToken);
        if (memberWorkspace == null)
        {
            return ServiceResult.Fail("Workspace not found.", HttpStatusCode.NotFound);
        }

        var workspace = await workspaceRepository.GetWorkspaceForOwnerAsync(id, currentUserId, cancellationToken);
        if (workspace == null)
        {
            return ServiceResult.Fail("Only workspace owner can update workspace.", HttpStatusCode.Forbidden);
        }

        workspace.Name = request.Name.Trim();
        workspace.UpdatedAt = DateTime.UtcNow;
        workspaceRepository.Update(workspace);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ArchiveWorkspaceAsync(int id, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        var memberWorkspace = await workspaceRepository.GetWorkspaceForMemberAsync(id, currentUserId, cancellationToken);
        if (memberWorkspace == null)
        {
            return ServiceResult.Fail("Workspace not found.", HttpStatusCode.NotFound);
        }

        var workspace = await workspaceRepository.GetWorkspaceForOwnerAsync(id, currentUserId, cancellationToken);
        if (workspace == null)
        {
            return ServiceResult.Fail("Only workspace owner can archive workspace.", HttpStatusCode.Forbidden);
        }

        workspace.IsArchived = true;
        workspace.UpdatedAt = DateTime.UtcNow;
        workspaceRepository.Update(workspace);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private static WorkspaceResponse MapToResponse(WorkSpace workspace)
    {
        return new WorkspaceResponse(
            workspace.Id,
            workspace.Name,
            workspace.IsArchived,
            workspace.CreatedByUserId,
            workspace.CreatedAt,
            workspace.UpdatedAt);
    }

    private static WorkspaceListItemResponse MapToListItemResponse(WorkSpace workspace)
    {
        return new WorkspaceListItemResponse(
            workspace.Id,
            workspace.Name,
            workspace.IsArchived,
            workspace.CreatedAt,
            workspace.UpdatedAt);
    }
}
