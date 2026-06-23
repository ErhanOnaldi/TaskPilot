namespace TaskPilot.Application.Interfaces.Security;

public interface IRefreshTokenGenerator
{
    string Generate();
}