using System.Net;
using FluentValidation;
using TaskPilot.Application.Features.Project.Dtos;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Application.Interfaces.Persistence.Project;
using TaskPilot.Application.Interfaces.Persistence.Workspace;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Project.Services;

public class ProjectService(
    IProjectRepository projectRepository,
    IProjectMemberRepository projectMemberRepository,
    IWorkspaceRepository workspaceRepository,
    IWorkspaceMemberRepository workspaceMemberRepository,
    IUnitOfWork unitOfWork, 
    ICurrentUserService currentUserService, 
    IValidator<CreateProjectRequest> createValidator,
    IValidator<UpdateProjectRequest> updateValidator) : IProjectService
{
    public async Task<ServiceResult<List<ProjectListItemResponse>>> GetProjectsAsync(int workspaceId, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        var workspace = await workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ServiceResult<List<ProjectListItemResponse>>.Fail("Workspace not found", HttpStatusCode.NotFound);
        }
        if (workspace.IsArchived)
        {
            return ServiceResult<List<ProjectListItemResponse>>.Fail("Workspace is archived", HttpStatusCode.BadRequest);
        }
        var workspaceMember = await workspaceMemberRepository.GetMemberAsync(workspaceId,currentUserId,cancellationToken);
        if (workspaceMember == null)
        {
            return ServiceResult<List<ProjectListItemResponse>>.Fail("Workspace member not found", HttpStatusCode.NotFound);
        }

        var projects = await projectRepository.GetProjectsByWorkspaceIdAsync(workspaceId, cancellationToken);
        var projectsAsDtos = projects.Select(x => new ProjectListItemResponse
        (
            x.Id,
            x.Name,
            x.Description,
            x.Status,
            x.CreatedAt,
            x.UpdatedAt
        )).ToList();
        return ServiceResult<List<ProjectListItemResponse>>.Success(projectsAsDtos);
    }

    public async Task<ServiceResult<ProjectResponse>> CreateProjectAsync(int workspaceId, CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await createValidator.ValidateAsync(request,cancellationToken);
        if (!validationResult.IsValid)
        {
            return ServiceResult<ProjectResponse>.Fail(
                validationResult.Errors.Select(x => x.ErrorMessage).ToList(),
                HttpStatusCode.BadRequest);
        }
        var currentUserId = currentUserService.UserId;
        var workspace = await workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ServiceResult<ProjectResponse>.Fail("Workspace not found", HttpStatusCode.NotFound);
        }
        if (workspace.IsArchived)
        {
            return ServiceResult<ProjectResponse>.Fail("Workspace is archived", HttpStatusCode.BadRequest);
        }
        var workspaceMember = await workspaceMemberRepository.GetMemberAsync(workspaceId,currentUserId,cancellationToken);
        if (workspaceMember == null)
        {
            return ServiceResult<ProjectResponse>.Fail("Workspace member not found", HttpStatusCode.NotFound);
        }

        if (workspaceMember.Role == Role.Guest || workspaceMember.Role == Role.Member)
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
            CreatedByUserId = currentUserId,
            Status = ProjectStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        project.Members.Add(new ProjectMember()
        {
            UserId = currentUserId,
            Project = project,
            Role = ProjectRole.ProjectManager,
            JoinedAt = DateTime.UtcNow
        });
        await projectRepository.AddAsync(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<ProjectResponse>.Success(new ProjectResponse(project.Id,project.WorkspaceId,project.Name,project.Description,project.Status,project.CreatedByUserId,project.CreatedAt,project.UpdatedAt),HttpStatusCode.Created);
    }

    public async Task<ServiceResult<ProjectResponse>> GetProjectAsync(int projectId, CancellationToken cancellationToken)
    {
        var currentUserId = currentUserService.UserId;
        var project = await projectRepository.GetProjectByIdAsync(projectId,cancellationToken);
        if (project == null)
        {
            return ServiceResult<ProjectResponse>.Fail("Project not found", HttpStatusCode.NotFound);
        }
        var isMember = await workspaceMemberRepository.IsWorkspaceMemberAsync(project.WorkspaceId,currentUserId,cancellationToken);
        if (!isMember)
        {
            return ServiceResult<ProjectResponse>.Fail("Project member not found", HttpStatusCode.NotFound);
        }
        return ServiceResult<ProjectResponse>.Success(new ProjectResponse(project.Id,project.WorkspaceId,project.Name,project.Description,project.Status,project.CreatedByUserId,project.CreatedAt,project.UpdatedAt));
    }

    public async Task<ServiceResult> UpdateProjectAsync(int projectId, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var validationResult = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return ServiceResult.Fail(
                validationResult.Errors.Select(x => x.ErrorMessage).ToList(),
                HttpStatusCode.BadRequest);
        }

        var currentUserId = currentUserService.UserId;
        var project = await projectRepository.GetProjectForUpdateAsync(projectId, cancellationToken);
        if (project == null)
        {
            return ServiceResult.Fail("Project not found", HttpStatusCode.NotFound);
        }

        if (project.Status == ProjectStatus.Archived)
        {
            return ServiceResult.Fail("Project is archived", HttpStatusCode.BadRequest);
        }

        var workspace = await workspaceRepository.GetByIdAsync(project.WorkspaceId);
        if (workspace == null)
        {
            return ServiceResult.Fail("Workspace not found", HttpStatusCode.NotFound);
        }

        if (workspace.IsArchived)
        {
            return ServiceResult.Fail("Workspace is archived", HttpStatusCode.BadRequest);
        }

        var workspaceMember = await workspaceMemberRepository.GetMemberAsync(project.WorkspaceId, currentUserId, cancellationToken);
        if (workspaceMember == null)
        {
            return ServiceResult.Fail("Project not found", HttpStatusCode.NotFound);
        }

        var isWorkspaceOwner = workspaceMember.Role == Role.Owner;
        var isProjectManager = await projectMemberRepository.IsProjectManagerAsync(projectId, currentUserId, cancellationToken);
        if (!isWorkspaceOwner && !isProjectManager)
        {
            return ServiceResult.Fail("Only workspace owner or project manager can update project", HttpStatusCode.Forbidden);
        }

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
        var currentUserId = currentUserService.UserId;
        var project = await projectRepository.GetProjectForUpdateAsync(projectId, cancellationToken);
        if (project == null)
        {
            return ServiceResult.Fail("Project not found", HttpStatusCode.NotFound);
        }

        var workspace = await workspaceRepository.GetByIdAsync(project.WorkspaceId);
        if (workspace == null)
        {
            return ServiceResult.Fail("Workspace not found", HttpStatusCode.NotFound);
        }

        if (workspace.IsArchived)
        {
            return ServiceResult.Fail("Workspace is archived", HttpStatusCode.BadRequest);
        }

        var workspaceMember = await workspaceMemberRepository.GetMemberAsync(project.WorkspaceId, currentUserId, cancellationToken);
        if (workspaceMember == null)
        {
            return ServiceResult.Fail("Project not found", HttpStatusCode.NotFound);
        }

        var isWorkspaceOwner = workspaceMember.Role == Role.Owner;
        var isProjectManager = await projectMemberRepository.IsProjectManagerAsync(projectId, currentUserId, cancellationToken);
        if (!isWorkspaceOwner && !isProjectManager)
        {
            return ServiceResult.Fail("Only workspace owner or project manager can archive project", HttpStatusCode.Forbidden);
        }

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
