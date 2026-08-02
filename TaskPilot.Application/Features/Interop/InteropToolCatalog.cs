using System.Text.Json;
using TaskPilot.Application.Common.Pagination;
using TaskPilot.Application.Features.Knowledge.Services;
using TaskPilot.Application.Features.Project.Dtos;
using TaskPilot.Application.Features.Project.Services;
using TaskPilot.Application.Features.Semantic.Dtos;
using TaskPilot.Application.Features.Semantic.Services;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Application.Features.Tasks.Services;

namespace TaskPilot.Application.Features.Interop;

public sealed class InteropToolCatalog(IProjectService projects, ITaskService tasks, IKnowledgeNoteService notes, ISemanticSearchService semantic) : IInteropToolCatalog
{
    private static readonly IReadOnlyList<InteropToolDefinition> Definitions = [new("projects.list", "List authorized workspace projects.", true), new("tasks.list", "List authorized project tasks.", true), new("tasks.create", "Create a task in an authorized project.", false), new("notes.read", "Read an authorized knowledge note.", true), new("semantic.search", "Search authorized semantic knowledge.", true)];
    public IReadOnlyList<InteropToolDefinition> List() => Definitions;
    public async Task<InteropToolResult> InvokeAsync(InteropToolCall call, CancellationToken ct)
    {
        try { return call.Name switch { "projects.list" => new(await projects.GetProjectsAsync(GetInt(call.Arguments, "workspaceId"), new ProjectQueryParameters(), ct)), "tasks.list" => new(await tasks.GetTasksAsync(GetInt(call.Arguments, "projectId"), new TaskQueryParameters(), ct)), "tasks.create" => new(await tasks.CreateTaskAsync(GetInt(call.Arguments, "projectId"), new CreateTaskRequest(GetString(call.Arguments, "title"), GetOptionalString(call.Arguments, "description"), null, null, null), ct)), "notes.read" => new(await notes.GetAsync(GetInt(call.Arguments, "noteId"), ct)), "semantic.search" => new(await semantic.SearchAsync(GetInt(call.Arguments, "workspaceId"), new SemanticSearchRequest(GetString(call.Arguments, "query")), ct)), _ => new(null, "Unknown or unsafe tool.") }; }
        catch (Exception exception) when (exception is KeyNotFoundException or JsonException) { return new(null, "Invalid tool arguments."); }
    }
    private static int GetInt(JsonElement e, string n) => e.GetProperty(n).GetInt32();
    private static string GetString(JsonElement e, string n) => e.GetProperty(n).GetString() ?? throw new JsonException();
    private static string? GetOptionalString(JsonElement e, string n) => e.TryGetProperty(n, out var value) ? value.GetString() : null;
}
