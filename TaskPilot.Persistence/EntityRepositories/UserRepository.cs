using Microsoft.EntityFrameworkCore;
using TaskPilot.Application.Interfaces.Persistence.User;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.EntityRepositories;

public class UserRepository(AppDbContext dbContext) : GenericRepository<User>(dbContext), IUserRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }
}
