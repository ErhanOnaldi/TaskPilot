using TaskPilot.Application.Interfaces.Infrastructure;

namespace TaskPilot.Infrastructure;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
