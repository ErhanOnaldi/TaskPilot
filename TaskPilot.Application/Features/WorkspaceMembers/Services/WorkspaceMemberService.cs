using System.Net;
using AutoMapper;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Features.WorkspaceMembers.Dtos;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.User;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.WorkspaceMembers.Services;

public class WorkspaceMemberService(
    IUnitOfWork unitOfWork,
    IAccessControlService accessControlService,
    IUserRepository userRepository,
    IWorkspaceMemberRepository workspaceMemberRepository,
    IMapper mapper) : IWorkspaceMemberService
{
    public async Task<ServiceResult<List<WorkspaceMemberResponse>>> GetMembersAsync(int workspaceId, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeWorkspaceAsync(
            workspaceId,
            WorkspaceAccessLevel.Member,
            requireActiveWorkspace: false,
            cancellationToken);
        if (access.Failure is not null)
        {
            return ServiceResult<List<WorkspaceMemberResponse>>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        var members = await workspaceMemberRepository.GetMembersByWorkspaceIdAsync(workspaceId, cancellationToken);
        return ServiceResult<List<WorkspaceMemberResponse>>.Success(
            mapper.Map<List<WorkspaceMemberResponse>>(members));
    }

    public async Task<ServiceResult<WorkspaceMemberResponse>> AddMemberAsync(int workspaceId, AddWorkspaceMemberRequest request, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeWorkspaceAsync(
            workspaceId,
            WorkspaceAccessLevel.Owner,
            requireActiveWorkspace: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult<WorkspaceMemberResponse>.Fail("Only workspace owner can add member.", HttpStatusCode.Forbidden)
                : ServiceResult<WorkspaceMemberResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        var userToAdd = await userRepository.GetByIdAsync(request.UserId);
        if (userToAdd == null)
        {
            return ServiceResult<WorkspaceMemberResponse>.Fail("User not found.", HttpStatusCode.NotFound);
        }
        var userAlreadyMember = await workspaceMemberRepository.IsWorkspaceMemberAsync(workspaceId, request.UserId, cancellationToken);
        if (userAlreadyMember)
        {
            return ServiceResult<WorkspaceMemberResponse>.Fail("User is already a workspace member.", HttpStatusCode.Conflict);
        }

        var workspaceMemberToAdd = new WorkspaceMember()
        {
            UserId = request.UserId,
            Role = request.Role,
            JoinedAt = DateTime.UtcNow,
            WorkspaceId = workspaceId
        };
        await workspaceMemberRepository.AddAsync(workspaceMemberToAdd);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        workspaceMemberToAdd.User = userToAdd;

        return ServiceResult<WorkspaceMemberResponse>.Success(
            mapper.Map<WorkspaceMemberResponse>(workspaceMemberToAdd),
            HttpStatusCode.Created);

    }

    public async Task<ServiceResult> UpdateMemberRoleAsync(int workspaceId, int userId, UpdateWorkspaceMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeWorkspaceAsync(
            workspaceId,
            WorkspaceAccessLevel.Owner,
            requireActiveWorkspace: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only workspace owner can update member role.", HttpStatusCode.Forbidden)
                : access.Failure;
        }

        var memberToUpdate = await workspaceMemberRepository.GetMemberAsync(workspaceId, userId, cancellationToken);
        if (memberToUpdate == null)
        {
            return ServiceResult.Fail("Member not found.", HttpStatusCode.NotFound);
        }
        
        if (memberToUpdate.Role == Role.Owner && request.Role != Role.Owner)
        {
            var ownerCount = await workspaceMemberRepository.CountOwnersAsync(workspaceId, cancellationToken);
            if (ownerCount == 1)
            {
                return ServiceResult.Fail("Workspace must have at least one owner.");
            }
        }
        memberToUpdate.Role = request.Role;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> RemoveMemberAsync(int workspaceId, int userId, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeWorkspaceAsync(
            workspaceId,
            WorkspaceAccessLevel.Owner,
            requireActiveWorkspace: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only workspace owner can remove member.", HttpStatusCode.Forbidden)
                : access.Failure;
        }

        var memberToDelete = await workspaceMemberRepository.GetMemberAsync(workspaceId, userId, cancellationToken);
        if (memberToDelete == null)
        {
            return ServiceResult.Fail("Member not found.", HttpStatusCode.NotFound);
        }

        if (memberToDelete.Role == Role.Owner)
        {
            var ownerCount = await workspaceMemberRepository.CountOwnersAsync(workspaceId, cancellationToken);
            if (ownerCount == 1)
            {
                return ServiceResult.Fail("Workspace must have at least one owner.");
            }
        }
        workspaceMemberRepository.Delete(memberToDelete);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }
}
