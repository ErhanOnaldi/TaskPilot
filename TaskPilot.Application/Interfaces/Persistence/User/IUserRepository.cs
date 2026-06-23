namespace TaskPilot.Application.Interfaces.Persistence.User;
using TaskPilot.Domain.Entities;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
