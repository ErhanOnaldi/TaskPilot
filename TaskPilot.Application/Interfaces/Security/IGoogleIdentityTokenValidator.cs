namespace TaskPilot.Application.Interfaces.Security;

public interface IGoogleIdentityTokenValidator
{
    /// <summary>False when no Google client id is configured for this deployment.</summary>
    bool IsEnabled { get; }

    /// <summary>Returns null when the token is malformed, expired, or issued for another audience.</summary>
    Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken);
}

public sealed record GoogleIdentity(string Subject, string Email, bool IsEmailVerified);
