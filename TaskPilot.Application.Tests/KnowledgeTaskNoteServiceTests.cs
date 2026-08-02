using System.Net;
using TaskPilot.Application;
using TaskPilot.Application.Features.Knowledge.Authorization;
using TaskPilot.Application.Features.Knowledge.Dtos;
using TaskPilot.Application.Features.Knowledge.Repositories;
using TaskPilot.Application.Features.Knowledge.Services;
using TaskPilot.Application.Features.Knowledge.Validators;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.Knowledge;

namespace TaskPilot.Application.Tests;

public sealed class KnowledgeTaskNoteServiceTests
{
    [Fact]
    public async Task LinkAsync_adds_once_when_active_task_and_note_share_scope()
    {
        var links = new FakeLinks();
        var uow = new FakeUnitOfWork();
        var service = CreateService(new FakeScope(new TaskNoteScope(10, 20, 30, 7, 7)), links, uow);

        var result = await service.LinkAsync(10, new TaskNoteLinkRequest(20, 30), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(links.Items);
        Assert.Equal(1, uow.SaveCount);
    }

    [Fact]
    public async Task LinkAsync_is_idempotent_when_link_already_exists()
    {
        var links = new FakeLinks { Existing = true };
        var uow = new FakeUnitOfWork();
        var service = CreateService(new FakeScope(new TaskNoteScope(10, 20, 30, 7, 7)), links, uow);

        var result = await service.LinkAsync(10, new TaskNoteLinkRequest(20, 30), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(links.Items);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task LinkAsync_rejects_cross_project_scope_before_mutation()
    {
        var links = new FakeLinks();
        var uow = new FakeUnitOfWork();
        var service = CreateService(new FakeScope(new TaskNoteScope(10, 20, 30, 7, 8)), links, uow);

        var result = await service.LinkAsync(10, new TaskNoteLinkRequest(20, 30), CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.Equal(0, uow.SaveCount);
    }

    [Fact]
    public async Task LinkAsync_rejects_guest_edit_access()
    {
        var service = new KnowledgeTaskNoteService(new FakeScope(new TaskNoteScope(10, 20, 30, 7, 7)), new FakeLinks(), new DeniedAccess(), new FakeUnitOfWork());

        var result = await service.LinkAsync(10, new TaskNoteLinkRequest(20, 30), CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.Forbidden, result.Status);
    }

    [Fact]
    public void TaskNoteLinkRequestValidator_rejects_empty_identifiers()
    {
        var result = new TaskNoteLinkRequestValidator().Validate(new TaskNoteLinkRequest(0, 0));
        Assert.False(result.IsValid);
    }

    private static KnowledgeTaskNoteService CreateService(FakeScope scope, FakeLinks links, FakeUnitOfWork uow) => new(scope, links, new AllowedAccess(), uow);

    private sealed class AllowedAccess : IKnowledgeAccessPort
    {
        public Task<KnowledgeAccessResult> AuthorizeAsync(int workspaceId, KnowledgeAccessLevel level, CancellationToken cancellationToken) => Task.FromResult(new KnowledgeAccessResult(1, null));
    }

    private sealed class DeniedAccess : IKnowledgeAccessPort
    {
        public Task<KnowledgeAccessResult> AuthorizeAsync(int workspaceId, KnowledgeAccessLevel level, CancellationToken cancellationToken) => Task.FromResult(KnowledgeAccessResult.Denied(ServiceResult.Fail("Guest access is read-only.", HttpStatusCode.Forbidden), 2));
    }

    private sealed class FakeScope(TaskNoteScope? scope) : IKnowledgeTaskNoteScopePort
    {
        public Task<TaskNoteScope?> GetActiveScopeAsync(int workspaceId, int taskId, int noteId, CancellationToken cancellationToken) => Task.FromResult(scope);
    }

    private sealed class FakeLinks : ITaskNoteLinkRepository
    {
        public bool Existing { get; init; }
        public List<TaskNoteLink> Items { get; } = [];
        public Task<bool> ExistsAsync(int taskId, int noteId, CancellationToken cancellationToken) => Task.FromResult(Existing);
        public ValueTask AddAsync(TaskNoteLink link, CancellationToken cancellationToken) { Items.Add(link); return ValueTask.CompletedTask; }
        public Task RemoveAsync(int taskId, int noteId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) { SaveCount++; return Task.FromResult(1); }
    }
}
