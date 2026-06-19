using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TaskPilot.Persistence.Extensions;

public static class PersistenceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("PostgreSql"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(
                        typeof(AppDbContext).Assembly.FullName);
                });
        });
        return services;
    }
}