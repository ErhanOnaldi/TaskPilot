namespace TaskPilot.Domain.Entities;

public class TaskLabel
{
    public int TaskId { get; set; }
    public int LabelId { get; set; }

    public TaskItem? TaskItem { get; set; }
    public Label? Label { get; set; }
}