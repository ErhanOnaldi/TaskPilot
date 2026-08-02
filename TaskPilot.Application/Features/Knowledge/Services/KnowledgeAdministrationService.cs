using System.Net;
using TaskPilot.Application.Features.Knowledge.Authorization;
using TaskPilot.Application.Features.Knowledge.Dtos;
using TaskPilot.Application.Features.Knowledge.Repositories;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.Knowledge;

namespace TaskPilot.Application.Features.Knowledge.Services;

public sealed class KnowledgeAdministrationService(
    IKnowledgeFolderRepository folderRepository,
    IKnowledgeTagRepository tagRepository,
    IKnowledgeScopeValidationPort scopeValidationPort,
    IKnowledgeAccessPort knowledgeAccessPort,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IKnowledgeAdministrationService
{
    public async Task<ServiceResult> CreateFolderAsync(int workspaceId, CreateKnowledgeFolderRequest request, CancellationToken cancellationToken)
    {
        var access = await knowledgeAccessPort.AuthorizeAsync(workspaceId, request.ProjectId, KnowledgeAccessLevel.Manage, cancellationToken);
        if (access.Failure is not null) return access.Failure;
        if (!await scopeValidationPort.IsProjectInWorkspaceAsync(workspaceId, request.ProjectId, cancellationToken) || !await scopeValidationPort.IsParentFolderInWorkspaceAsync(workspaceId, request.ProjectId, request.ParentFolderId, cancellationToken)) return ServiceResult.Fail("Project and parent folder must belong to the workspace.", HttpStatusCode.BadRequest);
        var slug = KnowledgeSlugPolicy.Normalize(request.Slug);
        if (await folderRepository.ExistsBySlugAsync(workspaceId, request.ProjectId, slug, cancellationToken)) return ServiceResult.Fail("A folder with this slug already exists.", HttpStatusCode.Conflict);
        var now = dateTimeProvider.UtcNow;
        await folderRepository.AddAsync(new KnowledgeFolder { WorkspaceId = workspaceId, ProjectId = request.ProjectId, ParentFolderId = request.ParentFolderId, Name = request.Name.Trim(), NormalizedSlug = slug, CreatedAt = now, UpdatedAt = now }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.Created);
    }

    public async Task<ServiceResult> CreateTagAsync(int workspaceId, CreateKnowledgeTagRequest request, CancellationToken cancellationToken)
    {
        var access = await knowledgeAccessPort.AuthorizeAsync(workspaceId, request.ProjectId, KnowledgeAccessLevel.Manage, cancellationToken);
        if (access.Failure is not null) return access.Failure;
        if (!await scopeValidationPort.IsProjectInWorkspaceAsync(workspaceId, request.ProjectId, cancellationToken)) return ServiceResult.Fail("Project must belong to the workspace.", HttpStatusCode.BadRequest);
        var slug = KnowledgeSlugPolicy.Normalize(request.Slug);
        if (await tagRepository.ExistsBySlugAsync(workspaceId, request.ProjectId, slug, cancellationToken)) return ServiceResult.Fail("A tag with this slug already exists.", HttpStatusCode.Conflict);
        var now = dateTimeProvider.UtcNow;
        await tagRepository.AddAsync(new KnowledgeTag { WorkspaceId = workspaceId, ProjectId = request.ProjectId, Name = request.Name.Trim(), NormalizedSlug = slug, CreatedAt = now, UpdatedAt = now }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.Created);
    }
}
