using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TaskPilot.Application.Interfaces.Security;
using TaskPilot.Domain.Options;

namespace TaskPilot.Infrastructure.Security;

public sealed class RefreshTokenHasher(IOptions<JwtOptions> options) : IRefreshTokenHasher
{
    public string Hash(string refreshToken)
    {
        var key = Encoding.UTF8.GetBytes(options.Value.Secret);
        var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);
        var hashBytes = HMACSHA256.HashData(key, tokenBytes);

        return Convert.ToBase64String(hashBytes);
    }
}
