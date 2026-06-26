using System.Net;
using FluentValidation;
using TaskPilot.Application.Features.WorkspaceMembers.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.User;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.WorkspaceMembers.Services;

public class WorkspaceMemberService(
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUserService,
    IWorkspaceRepository workspaceRepository,
    IUserRepository userRepository,
    IWorkspaceMemberRepository workspaceMemberRepository,
    IValidator<AddWorkspaceMemberRequest> createValidator,
    IValidator<UpdateWorkspaceMemberRoleRequest> updateValidator) : IWorkspaceMemberService
{
    public async Task<ServiceResult<List<WorkspaceMemberResponse>>> GetMembersAsync(int workspaceId, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.GetRequiredUserId();
        var workspace = await workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ServiceResult<List<WorkspaceMemberResponse>>.Fail("Workspace not found",HttpStatusCode.NotFound);
        }

        var isMember = await workspaceMemberRepository.IsWorkspaceMemberAsync(workspaceId, currentUserId, cancellationToken);
        if (!isMember)
        {
            return ServiceResult<List<WorkspaceMemberResponse>>.Fail("Workspace not found", HttpStatusCode.NotFound);
        }

        var members = await workspaceMemberRepository.GetMembersByWorkspaceIdAsync(workspaceId, cancellationToken);
        var response = members
            .Select(member => new WorkspaceMemberResponse(
                member.UserId,
                member.User?.Email ?? string.Empty,
                member.Role,
                member.JoinedAt))
            .ToList();

        return ServiceResult<List<WorkspaceMemberResponse>>.Success(response);
    }

    public async Task<ServiceResult<WorkspaceMemberResponse>> AddMemberAsync(int workspaceId, AddWorkspaceMemberRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ServiceResult<WorkspaceMemberResponse>.Fail(
                validationResult.Errors.Select(x => x.ErrorMessage).ToList());
        }
        var currentUserId = currentUserService.GetRequiredUserId();
        var workspace = await workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ServiceResult<WorkspaceMemberResponse>.Fail("Workspace not found", HttpStatusCode.NotFound);
        }

        if (workspace.IsArchived)
        {
            return ServiceResult<WorkspaceMemberResponse>.Fail("Workspace is archived.");
        }
        var isCurrentUserOwner = await workspaceMemberRepository.IsWorkspaceOwnerAsync(workspaceId, currentUserId, cancellationToken);
        if (!isCurrentUserOwner)
        {
            return ServiceResult<WorkspaceMemberResponse>.Fail("Only workspace owner can add member.", HttpStatusCode.Forbidden);
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
        return ServiceResult<WorkspaceMemberResponse>.Success(
            new WorkspaceMemberResponse(
                workspaceMemberToAdd.UserId,
                userToAdd.Email,
                workspaceMemberToAdd.Role,
                workspaceMemberToAdd.JoinedAt),
            HttpStatusCode.Created);

    }

    public async Task<ServiceResult> UpdateMemberRoleAsync(int workspaceId, int userId, UpdateWorkspaceMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ServiceResult.Fail(
                validationResult.Errors.Select(x => x.ErrorMessage).ToList());
        }
        var currentUserId = currentUserService.GetRequiredUserId();
        var workspace = await workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ServiceResult.Fail("Workspace not found.", HttpStatusCode.NotFound);
        }

        if (workspace.IsArchived)
        {
            return ServiceResult.Fail("Workspace is archived.");
        }

        var isCurrentUserOwner = await workspaceMemberRepository.IsWorkspaceOwnerAsync(workspaceId, currentUserId, cancellationToken);
        if (!isCurrentUserOwner)
        {
            return ServiceResult.Fail("Only workspace owner can update member role.", HttpStatusCode.Forbidden);
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
        var currentUserId = currentUserService.GetRequiredUserId();
        var workspace = await workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ServiceResult.Fail("Workspace not found.", HttpStatusCode.NotFound);
        }

        if (workspace.IsArchived)
        {
            return ServiceResult.Fail("Workspace is archived.");
        }

        var isCurrentUserOwner = await workspaceMemberRepository.IsWorkspaceOwnerAsync(workspaceId, currentUserId, cancellationToken);
        if (!isCurrentUserOwner)
        {
            return ServiceResult.Fail("Only workspace owner can remove member.", HttpStatusCode.Forbidden);
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
