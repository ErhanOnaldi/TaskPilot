namespace TaskPilot.Application.Interfaces.Security;

public interface IRefreshTokenHasher
{
    string Hash(string refreshToken);
}
