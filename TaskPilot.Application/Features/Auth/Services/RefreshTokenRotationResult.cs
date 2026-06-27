using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Features.Auth.Services;

public sealed record RefreshTokenRotationResult(
    bool IsSuccess,
    RefreshTokenRotationFailureReason? FailureReason,
    bool RequiresSaveChanges,
    User? User,
    CreatedRefreshToken? RefreshToken)
{
    public static RefreshTokenRotationResult Success(User user, CreatedRefreshToken refreshToken)
    {
        return new RefreshTokenRotationResult(true, null, true, user, refreshToken);
    }

    public static RefreshTokenRotationResult Fail(
        RefreshTokenRotationFailureReason reason,
        bool requiresSaveChanges = false)
    {
        return new RefreshTokenRotationResult(false, reason, requiresSaveChanges, null, null);
    }
}
