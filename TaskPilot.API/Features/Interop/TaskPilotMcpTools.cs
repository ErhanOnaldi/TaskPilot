using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using TaskPilot.Application.Features.Interop;

namespace TaskPilot.API.Features.Interop;

[McpServerToolType]
public sealed class TaskPilotMcpTools(IInteropToolCatalog catalog)
{
    [McpServerTool(Name = "projects.list", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("List projects the current user can access in a workspace.")]
    public Task<InteropToolResult> ListProjectsAsync(
        [Description("Workspace identifier.")] int workspaceId,
        CancellationToken cancellationToken) =>
        InvokeAsync("projects.list", new { workspaceId }, cancellationToken);

    [McpServerTool(Name = "tasks.list", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("List tasks the current user can access in a project.")]
    public Task<InteropToolResult> ListTasksAsync(
        [Description("Project identifier.")] int projectId,
        CancellationToken cancellationToken) =>
        InvokeAsync("tasks.list", new { projectId }, cancellationToken);

    [McpServerTool(Name = "tasks.create", ReadOnly = false, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Create a task in an authorized project. This tool never deletes or archives data.")]
    public Task<InteropToolResult> CreateTaskAsync(
        [Description("Project identifier.")] int projectId,
        [Description("Task title.")] string title,
        [Description("Optional task description.")] string? description,
        CancellationToken cancellationToken) =>
        InvokeAsync("tasks.create", new { projectId, title, description }, cancellationToken);

    [McpServerTool(Name = "notes.read", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Read a knowledge note the current user is authorized to access.")]
    public Task<InteropToolResult> ReadNoteAsync(
        [Description("Knowledge note identifier.")] int noteId,
        CancellationToken cancellationToken) =>
        InvokeAsync("notes.read", new { noteId }, cancellationToken);

    [McpServerTool(Name = "semantic.search", ReadOnly = true, Destructive = false, OpenWorld = false, UseStructuredContent = true)]
    [Description("Search authorized workspace knowledge and tasks semantically.")]
    public Task<InteropToolResult> SemanticSearchAsync(
        [Description("Workspace identifier.")] int workspaceId,
        [Description("Natural-language search query.")] string query,
        CancellationToken cancellationToken) =>
        InvokeAsync("semantic.search", new { workspaceId, query }, cancellationToken);

    private Task<InteropToolResult> InvokeAsync(string name, object arguments, CancellationToken cancellationToken)
    {
        var element = JsonSerializer.SerializeToElement(arguments);
        return catalog.InvokeAsync(new InteropToolCall(name, element), cancellationToken);
    }
}
