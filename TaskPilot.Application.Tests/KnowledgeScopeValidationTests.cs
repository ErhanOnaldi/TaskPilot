using System.Net;
using TaskPilot.Application;
using TaskPilot.Application.Features.Knowledge.Authorization;
using TaskPilot.Application.Features.Knowledge.Dtos;
using TaskPilot.Application.Features.Knowledge.Repositories;
using TaskPilot.Application.Features.Knowledge.Services;
using TaskPilot.Application.Features.Knowledge.Validators;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.Knowledge;

namespace TaskPilot.Application.Tests;

public sealed class KnowledgeScopeValidationTests
{
    [Fact]
    public async Task ListFoldersAsync_rejects_project_outside_workspace_before_repository_read()
    {
        var repository = new FakeFolderRepository();
        var service = new KnowledgeFolderService(repository, new InvalidProjectScope(), new AllowedAccess(), new FakeUnitOfWork(), new FakeClock());

        var result = await service.ListAsync(10, 99, CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.False(repository.ListCalled);
    }

    [Fact]
    public void UpdateKnowledgeFolderRequestValidator_rejects_empty_name_and_slug()
    {
        var result = new UpdateKnowledgeFolderRequestValidator().Validate(new UpdateKnowledgeFolderRequest("", "", null, null));
        Assert.False(result.IsValid);
    }

    private sealed class InvalidProjectScope : IKnowledgeScopeValidationPort
    {
        public Task<bool> IsProjectInWorkspaceAsync(int workspaceId, int? projectId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> IsFolderInWorkspaceAsync(int workspaceId, int? projectId, int? folderId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> IsParentFolderInWorkspaceAsync(int workspaceId, int? projectId, int? parentFolderId, CancellationToken cancellationToken) => Task.FromResult(false);
    }
    private sealed class AllowedAccess : IKnowledgeAccessPort { public Task<KnowledgeAccessResult> AuthorizeAsync(int workspaceId, KnowledgeAccessLevel accessLevel, CancellationToken cancellationToken) => Task.FromResult(new KnowledgeAccessResult(1, null)); }
    private sealed class FakeClock : IDateTimeProvider { public DateTime UtcNow => new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc); }
    private sealed class FakeUnitOfWork : IUnitOfWork { public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1); }
    private sealed class FakeFolderRepository : IKnowledgeFolderRepository
    {
        public bool ListCalled { get; private set; }
        public ValueTask<KnowledgeFolder?> GetByIdAsync(int folderId, CancellationToken cancellationToken) => ValueTask.FromResult<KnowledgeFolder?>(null);
        public Task<IReadOnlyList<KnowledgeFolder>> GetByWorkspaceAsync(int workspaceId, int? projectId, CancellationToken cancellationToken) { ListCalled = true; return Task.FromResult<IReadOnlyList<KnowledgeFolder>>([]); }
        public Task<bool> ExistsBySlugAsync(int workspaceId, int? projectId, string normalizedSlug, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> ExistsBySlugExceptFolderAsync(int workspaceId, int? projectId, int folderIdToExclude, string normalizedSlug, CancellationToken cancellationToken) => Task.FromResult(false);
        public ValueTask AddAsync(KnowledgeFolder folder, CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public Task DeleteAsync(KnowledgeFolder folder, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
