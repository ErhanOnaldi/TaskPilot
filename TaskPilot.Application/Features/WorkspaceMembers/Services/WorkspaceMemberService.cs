using System.Net;
using AutoMapper;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Features.WorkspaceMembers.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.User;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Tasks;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;
using TaskPilot.Application.Events;

namespace TaskPilot.Application.Features.WorkspaceMembers.Services;

public class WorkspaceMemberService(
    IUnitOfWork unitOfWork,
    IAccessControlService accessControlService,
    IUserRepository userRepository,
    IWorkspaceMemberRepository workspaceMemberRepository,
    IProjectMemberRepository projectMemberRepository,
    ITaskRepository taskRepository,
    IMapper mapper,
    IEventOutbox eventOutbox,
    IDateTimeProvider dateTimeProvider) : IWorkspaceMemberService
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
            WorkspaceAccessLevel.Invite,
            requireActiveWorkspace: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult<WorkspaceMemberResponse>.Fail("Only workspace owner or manager can add member.", HttpStatusCode.Forbidden)
                : ServiceResult<WorkspaceMemberResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        if (access.WorkspaceMember.Role == Role.Manager && request.Role != Role.Member)
        {
            return ServiceResult<WorkspaceMemberResponse>.Fail(
                "Workspace managers can invite only team members.",
                HttpStatusCode.Forbidden);
        }

        var userToAdd = await ResolveInviteeAsync(request, cancellationToken);
        if (userToAdd == null)
        {
            return ServiceResult<WorkspaceMemberResponse>.Fail("User not found.", HttpStatusCode.NotFound);
        }
        var userAlreadyMember = await workspaceMemberRepository.IsWorkspaceMemberAsync(workspaceId, userToAdd.Id, cancellationToken);
        if (userAlreadyMember)
        {
            return ServiceResult<WorkspaceMemberResponse>.Fail("User is already a workspace member.", HttpStatusCode.Conflict);
        }

        var workspaceMemberToAdd = new WorkspaceMember()
        {
            UserId = userToAdd.Id,
            Role = request.Role,
            JoinedAt = dateTimeProvider.UtcNow,
            WorkspaceId = workspaceId
        };
        await workspaceMemberRepository.AddAsync(workspaceMemberToAdd);
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            await eventOutbox.EnqueueAsync(
                () => new WorkspaceMemberInvitedEvent(
                    EventId: Guid.NewGuid(),
                    WorkspaceId: workspaceId,
                    InvitedUserId: userToAdd.Id,
                    InvitedByUserId: access.CurrentUserId,
                    OccurredAt: dateTimeProvider.UtcNow),
                cancellationToken);
        }
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

        if (request.Role == Role.Guest && memberToUpdate.Role != Role.Guest)
        {
            var preparationFailure = await PrepareProjectMembershipsAsync(
                workspaceId,
                userId,
                removeMemberships: false,
                cancellationToken);
            if (preparationFailure is not null)
            {
                return preparationFailure;
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

        var preparationFailure = await PrepareProjectMembershipsAsync(
            workspaceId,
            userId,
            removeMemberships: true,
            cancellationToken);
        if (preparationFailure is not null)
        {
            return preparationFailure;
        }

        workspaceMemberRepository.Delete(memberToDelete);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    private async Task<User?> ResolveInviteeAsync(
        AddWorkspaceMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            return await userRepository.GetByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken);
        }

#pragma warning disable CS0618
        return request.UserId.HasValue
            ? await userRepository.GetByIdAsync(request.UserId.Value)
            : null;
#pragma warning restore CS0618
    }

    private async Task<ServiceResult?> PrepareProjectMembershipsAsync(
        int workspaceId,
        int userId,
        bool removeMemberships,
        CancellationToken cancellationToken)
    {
        var memberships = await projectMemberRepository.GetUserMembershipsByWorkspaceAsync(
            workspaceId,
            userId,
            cancellationToken);

        foreach (var membership in memberships.Where(member => member.Role == ProjectRole.ProjectManager))
        {
            if (await projectMemberRepository.CountProjectManagersAsync(membership.ProjectId, cancellationToken) <= 1)
            {
                return ServiceResult.Fail(
                    "Assign another project manager before removing or downgrading this workspace member.",
                    HttpStatusCode.BadRequest);
            }
        }

        await taskRepository.UnassignUserFromWorkspaceAsync(
            workspaceId,
            userId,
            dateTimeProvider.UtcNow,
            cancellationToken);

        foreach (var membership in memberships)
        {
            if (removeMemberships)
            {
                projectMemberRepository.Delete(membership);
            }
            else
            {
                membership.Role = ProjectRole.Guest;
            }
        }

        return null;
    }
}
