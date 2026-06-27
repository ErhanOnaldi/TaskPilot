using System.Net;
using AutoMapper;
using TaskPilot.Application.Authorization;
using TaskPilot.Application.Features.Project.Dtos;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Project.Services;

public class ProjectService(
    IProjectRepository projectRepository,
    IUnitOfWork unitOfWork, 
    IAccessControlService accessControlService,
    IMapper mapper) : IProjectService
{
    public async Task<ServiceResult<List<ProjectListItemResponse>>> GetProjectsAsync(int workspaceId, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeWorkspaceAsync(
            workspaceId,
            WorkspaceAccessLevel.Member,
            requireActiveWorkspace: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return ServiceResult<List<ProjectListItemResponse>>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        var projects = await projectRepository.GetProjectsByWorkspaceIdAsync(workspaceId, cancellationToken);
        return ServiceResult<List<ProjectListItemResponse>>.Success(
            mapper.Map<List<ProjectListItemResponse>>(projects));
    }

    public async Task<ServiceResult<ProjectResponse>> CreateProjectAsync(int workspaceId, CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeWorkspaceAsync(
            workspaceId,
            WorkspaceAccessLevel.Member,
            requireActiveWorkspace: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return ServiceResult<ProjectResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        if (access.WorkspaceMember.Role is Role.Guest or Role.Member)
        {
            return ServiceResult<ProjectResponse>.Fail("Member not authorized", HttpStatusCode.Forbidden);
        }
        var name = request.Name.Trim();
        if (await projectRepository.ExistsByNameInWorkspaceAsync(workspaceId, name, cancellationToken))
        {
            return ServiceResult<ProjectResponse>.Fail("Project already exists", HttpStatusCode.Conflict);
        }

        var project = new Domain.Entities.Project
        {
            WorkspaceId = workspaceId,
            Name = name,
            Description = request.Description?.Trim(),
            CreatedByUserId = access.CurrentUserId,
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        project.Members.Add(new ProjectMember()
        {
            UserId = access.CurrentUserId,
            Project = project,
            Role = ProjectRole.ProjectManager,
            JoinedAt = DateTime.UtcNow
        });
        await projectRepository.AddAsync(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<ProjectResponse>.Success(mapper.Map<ProjectResponse>(project), HttpStatusCode.Created);
    }

    public async Task<ServiceResult<ProjectResponse>> GetProjectAsync(int projectId, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeProjectAsync(
            projectId,
            ProjectAccessLevel.Read,
            requireActiveProject: false,
            cancellationToken);
        if (access.Failure is not null)
        {
            return ServiceResult<ProjectResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        }

        return ServiceResult<ProjectResponse>.Success(mapper.Map<ProjectResponse>(access.Project));
    }

    public async Task<ServiceResult> UpdateProjectAsync(int projectId, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeProjectAsync(
            projectId,
            ProjectAccessLevel.Manage,
            requireActiveProject: true,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only workspace owner or project manager can update project", HttpStatusCode.Forbidden)
                : access.Failure;
        }

        var project = access.Project;
        var name = request.Name.Trim();
        var nameExists = await projectRepository.ExistsByNameInWorkspaceExceptProjectAsync(
            project.WorkspaceId,
            project.Id,
            name,
            cancellationToken);
        if (nameExists)
        {
            return ServiceResult.Fail("Project already exists", HttpStatusCode.Conflict);
        }

        project.Name = name;
        project.Description = request.Description?.Trim();
        project.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    public async Task<ServiceResult> ArchiveProjectAsync(int projectId, CancellationToken cancellationToken)
    {
        var access = await accessControlService.AuthorizeProjectAsync(
            projectId,
            ProjectAccessLevel.Manage,
            requireActiveProject: false,
            cancellationToken);
        if (access.Failure is not null)
        {
            return access.Failure.Status == HttpStatusCode.Forbidden
                ? ServiceResult.Fail("Only workspace owner or project manager can archive project", HttpStatusCode.Forbidden)
                : access.Failure;
        }

        var project = access.Project;
        if (project.Status == ProjectStatus.Archived)
        {
            return ServiceResult.Success(HttpStatusCode.NoContent);
        }

        project.Status = ProjectStatus.Archived;
        project.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }
}
