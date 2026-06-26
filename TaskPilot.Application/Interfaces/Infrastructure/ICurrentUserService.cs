namespace TaskPilot.Application.Interfaces.Infrastructure;

public interface ICurrentUserService
{
    int? UserId { get; }
    bool IsAuthenticated { get; }
    int GetRequiredUserId();
}
