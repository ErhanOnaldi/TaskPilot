namespace TaskPilot.Domain.Opitons;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = null!;
    public string Audience { get; init; } = null!;
    public string Secret { get; init; } = null!;
    public int AccessTokenExpirationMinutes { get; init; }
}