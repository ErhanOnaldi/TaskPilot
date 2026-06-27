using System.Net;
using AutoMapper;
using TaskPilot.Application.Authorization;
using TaskPilot.Application.Features.Workspace.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Workspace.Services;

public class WorkspaceService(
    ICurrentUserService currentUserService,
    IWorkspaceRepository workspaceRepository,
    IAccessControlService accessControlService,
    IUnitOfWork unitOfWork,
    IMapper mapper) : IWorkspaceService
{
    public async Task<ServiceResult<WorkspaceResponse>> CreateWorkspaceAsync(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
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
            mapper.Map<WorkspaceResponse>(workspace),
            $"api/workspaces/{workspace.Id}");
    }

    public async Task<ServiceResult<List<WorkspaceListItemResponse>>> GetWorkSpacesAsync(CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetRequiredUserId();
        var workspaces = await workspaceRepository.GetWorkspacesByUserIdAsync(userId, cancellationToken);

        return ServiceResult<List<WorkspaceListItemResponse>>.Success(
            mapper.Map<List<WorkspaceListItemResponse>>(workspaces));
    }

    public async Task<ServiceResult<WorkspaceResponse>> GetWorkspaceAsync(int id, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeWorkspaceAsync(
            id,
            WorkspaceAccessLevel.Member,
            requireActiveWorkspace: false,
            cancellationToken);
        if (access.Failure is not null)
        {
            return ServiceResult<WorkspaceResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        return ServiceResult<WorkspaceResponse>.Success(mapper.Map<WorkspaceResponse>(access.Workspace));
    }

    public async Task<ServiceResult> UpdateWorkspaceAsync(int id, UpdateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeWorkspaceAsync(
            id,
            WorkspaceAccessLevel.Owner,
            requireActiveWorkspace: false,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only workspace owner can update workspace.", HttpStatusCode.Forbidden)
                : access.Failure;
        }

        var workspace = access.Workspace;
        workspace.Name = request.Name.Trim();
        workspace.UpdatedAt = DateTime.UtcNow;
        workspaceRepository.Update(workspace);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ArchiveWorkspaceAsync(int id, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeWorkspaceAsync(
            id,
            WorkspaceAccessLevel.Owner,
            requireActiveWorkspace: false,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only workspace owner can archive workspace.", HttpStatusCode.Forbidden)
                : access.Failure;
        }

        var workspace = access.Workspace;
        workspace.IsArchived = true;
        workspace.UpdatedAt = DateTime.UtcNow;
        workspaceRepository.Update(workspace);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

}
