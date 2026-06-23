using System.Security.Cryptography;
using TaskPilot.Application.Interfaces.Security;

namespace TaskPilot.Infrastructure.Security;

public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    public string Generate()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }
}
