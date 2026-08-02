using System.Net;
using TaskPilot.Application.Features.Knowledge.Authorization;
using TaskPilot.Application.Features.Knowledge.Dtos;
using TaskPilot.Application.Features.Knowledge.Repositories;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.Knowledge;

namespace TaskPilot.Application.Features.Knowledge.Services;

public sealed class KnowledgeFolderService(
    IKnowledgeFolderRepository folderRepository,
    IKnowledgeScopeValidationPort scopeValidationPort,
    IKnowledgeAccessPort accessPort,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : IKnowledgeFolderService
{
    public async Task<ServiceResult<IReadOnlyList<KnowledgeFolderResponse>>> ListAsync(
        int workspaceId,
        int? projectId,
        CancellationToken cancellationToken)
    {
        var access = await accessPort.AuthorizeAsync(workspaceId, projectId, KnowledgeAccessLevel.Read, cancellationToken);
        if (access.Failure is not null)
            return ServiceResult<IReadOnlyList<KnowledgeFolderResponse>>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
        if (!await scopeValidationPort.IsProjectInWorkspaceAsync(workspaceId, projectId, cancellationToken))
            return ServiceResult<IReadOnlyList<KnowledgeFolderResponse>>.Fail("Project must belong to the workspace.", HttpStatusCode.BadRequest);
        var folders = await folderRepository.GetByWorkspaceAsync(workspaceId, projectId, cancellationToken);
        return ServiceResult<IReadOnlyList<KnowledgeFolderResponse>>.Success(folders.Select(ToResponse).ToList());
    }

    public async Task<ServiceResult<KnowledgeFolderResponse>> GetAsync(int folderId, CancellationToken cancellationToken)
    {
        var folder = await folderRepository.GetByIdAsync(folderId, cancellationToken);
        if (folder is null)
            return ServiceResult<KnowledgeFolderResponse>.Fail("Folder not found.", HttpStatusCode.NotFound);
        var access = await accessPort.AuthorizeAsync(folder.WorkspaceId, folder.ProjectId, KnowledgeAccessLevel.Read, cancellationToken);
        return access.Failure is null
            ? ServiceResult<KnowledgeFolderResponse>.Success(ToResponse(folder))
            : ServiceResult<KnowledgeFolderResponse>.Fail(access.Failure.ErrorMessages!, access.Failure.Status);
    }

    public async Task<ServiceResult<KnowledgeFolderResponse>> UpdateAsync(
        int folderId,
        UpdateKnowledgeFolderRequest request,
        CancellationToken cancellationToken)
    {
        var folder = await folderRepository.GetByIdAsync(folderId, cancellationToken);
        if (folder is null)
            return ServiceResult<KnowledgeFolderResponse>.Fail("Folder not found.", HttpStatusCode.NotFound);
        var sourceAccess = await accessPort.AuthorizeAsync(folder.WorkspaceId, folder.ProjectId, KnowledgeAccessLevel.Manage, cancellationToken);
        if (sourceAccess.Failure is not null)
            return ServiceResult<KnowledgeFolderResponse>.Fail(sourceAccess.Failure.ErrorMessages!, sourceAccess.Failure.Status);
        var targetAccess = await accessPort.AuthorizeAsync(folder.WorkspaceId, request.ProjectId, KnowledgeAccessLevel.Manage, cancellationToken);
        if (targetAccess.Failure is not null)
            return ServiceResult<KnowledgeFolderResponse>.Fail(targetAccess.Failure.ErrorMessages!, targetAccess.Failure.Status);
        if (!await scopeValidationPort.IsProjectInWorkspaceAsync(folder.WorkspaceId, request.ProjectId, cancellationToken) ||
            !await scopeValidationPort.IsParentFolderInWorkspaceAsync(folder.WorkspaceId, request.ProjectId, request.ParentFolderId, cancellationToken))
            return ServiceResult<KnowledgeFolderResponse>.Fail("Project and parent folder must belong to the workspace.", HttpStatusCode.BadRequest);

        var slug = KnowledgeSlugPolicy.Normalize(request.Slug);
        if (await folderRepository.ExistsBySlugExceptFolderAsync(folder.WorkspaceId, request.ProjectId, folder.Id, slug, cancellationToken))
            return ServiceResult<KnowledgeFolderResponse>.Fail("A folder with this slug already exists.", HttpStatusCode.Conflict);
        folder.Name = request.Name.Trim();
        folder.NormalizedSlug = slug;
        folder.ProjectId = request.ProjectId;
        folder.ParentFolderId = request.ParentFolderId;
        folder.UpdatedAt = dateTimeProvider.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<KnowledgeFolderResponse>.Success(ToResponse(folder));
    }

    public async Task<ServiceResult> DeleteAsync(int folderId, CancellationToken cancellationToken)
    {
        var folder = await folderRepository.GetByIdAsync(folderId, cancellationToken);
        if (folder is null) return ServiceResult.Fail("Folder not found.", HttpStatusCode.NotFound);
        var access = await accessPort.AuthorizeAsync(folder.WorkspaceId, folder.ProjectId, KnowledgeAccessLevel.Manage, cancellationToken);
        if (access.Failure is not null) return access.Failure;
        await folderRepository.DeleteAsync(folder, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success(HttpStatusCode.NoContent);
    }

    private static KnowledgeFolderResponse ToResponse(KnowledgeFolder folder) => new(
        folder.Id, folder.WorkspaceId, folder.ProjectId, folder.ParentFolderId,
        folder.Name, folder.NormalizedSlug, folder.UpdatedAt);
}
