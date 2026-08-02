namespace TaskPilot.Domain.AI.Copilot;

public enum CopilotMessageRole { User, Assistant, System }
public enum CopilotCitationType { Task, Note }

public sealed class CopilotChatSession
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public int? ProjectId { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public ICollection<CopilotChatMessage> Messages { get; set; } = new List<CopilotChatMessage>();
}

public sealed class CopilotChatMessage
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public CopilotMessageRole Role { get; set; }
    public string Content { get; set; } = null!;
    public string CitationsJson { get; set; } = "[]";
    public DateTime CreatedAtUtc { get; set; }
}

public sealed record CopilotCitation(CopilotCitationType Type, int SourceId);
public sealed record CopilotContextItem(CopilotCitation Citation, string Content);
