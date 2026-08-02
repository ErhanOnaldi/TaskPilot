using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.API.Controllers;
using TaskPilot.Application.Features.Knowledge.Authorization;
using TaskPilot.Application.Features.Knowledge.Dtos;
using TaskPilot.Application.Features.Knowledge.ReadModels;
using TaskPilot.Application.Features.Knowledge.Services;
using TaskPilot.Application.Features.Semantic.Dtos;
using TaskPilot.Application.Features.Semantic.Services;

namespace TaskPilot.API.Features.Knowledge;

[ApiController]
[Authorize]
[Route("api")]
public sealed class KnowledgeController(
    IKnowledgeNoteService noteService,
    IKnowledgeAdministrationService administrationService,
    IKnowledgeFolderService folderService,
    IKnowledgeTaskNoteService taskNoteService,
    IKnowledgeReadPort readPort,
    IKnowledgeAccessPort accessPort,
    ISemanticSearchService semanticSearchService) : CustomBaseController
{
    [HttpPost("workspaces/{workspaceId:int}/folders")]
    public async Task<IActionResult> CreateFolder(int workspaceId, CreateKnowledgeFolderRequest request, CancellationToken cancellationToken) => CreateActionResult(await administrationService.CreateFolderAsync(workspaceId, request, cancellationToken));

    [HttpGet("workspaces/{workspaceId:int}/folders")]
    public async Task<IActionResult> ListFolders(int workspaceId, [FromQuery] int? projectId, CancellationToken cancellationToken) => CreateActionResult(await folderService.ListAsync(workspaceId, projectId, cancellationToken));

    [HttpGet("folders/{folderId:int}")]
    public async Task<IActionResult> GetFolder(int folderId, CancellationToken cancellationToken) => CreateActionResult(await folderService.GetAsync(folderId, cancellationToken));

    [HttpPut("folders/{folderId:int}")]
    public async Task<IActionResult> UpdateFolder(int folderId, UpdateKnowledgeFolderRequest request, CancellationToken cancellationToken) => CreateActionResult(await folderService.UpdateAsync(folderId, request, cancellationToken));

    [HttpDelete("folders/{folderId:int}")]
    public async Task<IActionResult> DeleteFolder(int folderId, CancellationToken cancellationToken) => CreateActionResult(await folderService.DeleteAsync(folderId, cancellationToken));

    [HttpPost("workspaces/{workspaceId:int}/knowledge/tags")]
    public async Task<IActionResult> CreateTag(int workspaceId, CreateKnowledgeTagRequest request, CancellationToken cancellationToken) => CreateActionResult(await administrationService.CreateTagAsync(workspaceId, request, cancellationToken));

    [HttpPost("workspaces/{workspaceId:int}/notes")]
    public async Task<IActionResult> CreateNote(int workspaceId, CreateNoteRequest request, CancellationToken cancellationToken) => CreateActionResult(await noteService.CreateAsync(workspaceId, request, cancellationToken));

    [HttpGet("notes/{noteId:int}")]
    public async Task<IActionResult> GetNote(int noteId, CancellationToken cancellationToken) => CreateActionResult(await noteService.GetAsync(noteId, cancellationToken));

    [HttpPut("notes/{noteId:int}")]
    public async Task<IActionResult> UpdateNote(int noteId, UpdateNoteRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetExpectedVersion(out var version)) return PreconditionRequired();
        return CreateActionResult(await noteService.UpdateAsync(noteId, request with { ExpectedVersion = version }, cancellationToken));
    }

    [HttpDelete("notes/{noteId:int}")]
    public async Task<IActionResult> DeleteNote(int noteId, CancellationToken cancellationToken) => CreateActionResult(await noteService.DeleteAsync(noteId, cancellationToken));

    [HttpGet("notes/{noteId:int}/links")]
    public async Task<IActionResult> GetLinks(int noteId, CancellationToken cancellationToken) => await ReadForNoteAsync(noteId, workspaceId => readPort.GetLinksAsync(workspaceId, noteId, cancellationToken), cancellationToken);

    [HttpGet("notes/{noteId:int}/backlinks")]
    public async Task<IActionResult> GetBacklinks(int noteId, CancellationToken cancellationToken) => await ReadForNoteAsync(noteId, workspaceId => readPort.GetBacklinksAsync(workspaceId, noteId, cancellationToken), cancellationToken);

    [HttpGet("notes/{noteId:int}/revisions")]
    public async Task<IActionResult> GetRevisions(int noteId, CancellationToken cancellationToken) => await ReadForNoteAsync(noteId, workspaceId => readPort.GetRevisionsAsync(workspaceId, noteId, cancellationToken), cancellationToken);

    [HttpPost("notes/{noteId:int}/revisions/{revisionId:int}/restore")]
    public async Task<IActionResult> RestoreRevision(int noteId, int revisionId, CancellationToken cancellationToken)
    {
        if (!TryGetExpectedVersion(out var version)) return PreconditionRequired();
        return CreateActionResult(await noteService.RestoreRevisionAsync(noteId, revisionId, version, cancellationToken));
    }

    [HttpGet("workspaces/{workspaceId:int}/knowledge/search")]
    public async Task<IActionResult> Search(
        int workspaceId,
        [FromQuery(Name = "q")] string query,
        [FromQuery] string mode = "hybrid",
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<SemanticSearchMode>(mode, true, out var searchMode))
        {
            return BadRequest(new { message = "mode must be text, semantic, or hybrid." });
        }

        return CreateActionResult(await semanticSearchService.SearchAsync(
            workspaceId,
            new SemanticSearchRequest(query, searchMode, limit),
            cancellationToken));
    }

    [HttpGet("workspaces/{workspaceId:int}/knowledge/graph")]
    public async Task<IActionResult> GetGraph(int workspaceId, [FromQuery] int? projectId, CancellationToken cancellationToken) => await ReadAsync(workspaceId, projectId, () => readPort.GetGraphAsync(workspaceId, projectId, cancellationToken), cancellationToken);

    [HttpPost("tasks/{taskId:int}/notes/{noteId:int}")]
    public async Task<IActionResult> LinkTaskNote(int taskId, int noteId, CancellationToken cancellationToken)
    {
        var note = await noteService.GetAsync(noteId, cancellationToken);
        return note.IsFail
            ? CreateActionResult(note)
            : CreateActionResult(await taskNoteService.LinkAsync(note.Data!.WorkspaceId, new TaskNoteLinkRequest(taskId, noteId), cancellationToken));
    }

    [HttpDelete("tasks/{taskId:int}/notes/{noteId:int}")]
    public async Task<IActionResult> UnlinkTaskNote(int taskId, int noteId, CancellationToken cancellationToken)
    {
        var note = await noteService.GetAsync(noteId, cancellationToken);
        return note.IsFail
            ? CreateActionResult(note)
            : CreateActionResult(await taskNoteService.UnlinkAsync(note.Data!.WorkspaceId, new TaskNoteLinkRequest(taskId, noteId), cancellationToken));
    }

    private async Task<IActionResult> ReadAsync<T>(int workspaceId, int? projectId, Func<Task<T>> read, CancellationToken cancellationToken)
    {
        var access = await accessPort.AuthorizeAsync(workspaceId, projectId, KnowledgeAccessLevel.Read, cancellationToken);
        return access.Failure is null ? Ok(await read()) : CreateActionResult(access.Failure);
    }

    private async Task<IActionResult> ReadForNoteAsync<T>(
        int noteId,
        Func<int, Task<T>> read,
        CancellationToken cancellationToken)
    {
        var note = await noteService.GetAsync(noteId, cancellationToken);
        return note.IsFail
            ? CreateActionResult(note)
            : Ok(await read(note.Data!.WorkspaceId));
    }

    private bool TryGetExpectedVersion(out int version)
    {
        var raw = Request.Headers.IfMatch.ToString().Trim().Trim('"');
        return int.TryParse(raw, out version) && version > 0;
    }

    private IActionResult PreconditionRequired() => StatusCode(StatusCodes.Status428PreconditionRequired, new { message = "If-Match with the current note version is required." });
}
