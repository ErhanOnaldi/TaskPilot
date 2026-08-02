namespace TaskPilot.Domain.Policies;

public static class TaskCreationPolicy
{
    public const int MaximumTitleCharacters = 100;

    public static bool IsValid(string? title, DateTime? dueDate, DateTime utcNow) =>
        !string.IsNullOrWhiteSpace(title) &&
        title.Length <= MaximumTitleCharacters &&
        (dueDate is null || dueDate.Value.Date >= utcNow.Date);
}
