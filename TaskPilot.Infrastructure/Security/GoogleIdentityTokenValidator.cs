using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Domain.Options;

namespace TaskPilot.Infrastructure.Security;

public sealed class GoogleIdentityTokenValidator(
    IOptions<GoogleAuthOptions> options,
    ILogger<GoogleIdentityTokenValidator> logger) : IGoogleIdentityTokenValidator
{
    private readonly GoogleAuthOptions _options = options.Value;

    public bool IsEnabled => _options.IsEnabled;

    public async Task<GoogleIdentity?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            // Verifies the Google signature, issuer, expiry, and that the token was minted for this client id.
            var payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [_options.ClientId!]
                });

            return string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email)
                ? null
                : new GoogleIdentity(payload.Subject, payload.Email, payload.EmailVerified);
        }
        catch (InvalidJwtException exception)
        {
            logger.LogWarning(exception, "Rejected a Google id token that failed validation.");
            return null;
        }
    }
}
