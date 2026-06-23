using Microsoft.AspNetCore.Identity;
using TaskPilot.Application.Interfaces.Security;

namespace TaskPilot.Infrastructure.Security;

public sealed class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<object> _passwordHasher = new();

    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(null!, password);
    }

    public bool Verify(string password, string passwordHash)
    {
        var result = _passwordHasher.VerifyHashedPassword(null!, passwordHash, password);

        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}