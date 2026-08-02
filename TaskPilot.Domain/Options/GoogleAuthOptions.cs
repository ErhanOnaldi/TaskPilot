namespace TaskPilot.Domain.Options;

public sealed class GoogleAuthOptions
{
    public string? ClientId { get; init; }

    public bool IsEnabled => !string.IsNullOrWhiteSpace(ClientId);
}
