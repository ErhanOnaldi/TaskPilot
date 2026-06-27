namespace TaskPilot.Application.Interfaces.Infrastructure;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
