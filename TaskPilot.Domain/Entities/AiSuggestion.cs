namespace TaskPilot.Domain.Entities;

public class AiSuggestion
{
    public int Id { get; set; }
    public int? TaskId { get; set; }
    public int ProjectId { get; set; }
    public int RequestedByUserId { get; set; }
    public string InputText { get; set; } = null!;
    public string? SuggestedPriority { get; set; }
    public string? SuggestedLabels { get; set; }
    public string? SuggestedSubtasks { get; set; }
    public DateTime? SuggestedDueDate { get; set; }
    public string Status { get; set; } = "Pending";
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public TaskItem? TaskItem { get; set; }
    public Project? Project { get; set; }
    public User? RequestedByUser { get; set; }
}
