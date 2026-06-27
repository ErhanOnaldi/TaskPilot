namespace TaskPilot.Application.Features.Auth.Services;

public enum RefreshTokenRotationFailureReason
{
    Invalid,
    Expired,
    UserNotFound
}
