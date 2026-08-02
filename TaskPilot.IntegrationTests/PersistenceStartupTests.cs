using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Persistence;

namespace TaskPilot.IntegrationTests;

[Collection(TaskPilotIntegrationTestCollection.Name)]
public sealed class PersistenceStartupTests(TaskPilotIntegrationEnvironment environment)
{
    [Fact]
    public async Task Startup_applies_all_migrations_and_enables_pgvector()
    {
        using var client = environment.Factory.CreateClient();
        _ = await client.GetAsync("/health/live");

        await using var scope = environment.Factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

        Assert.Empty(pendingMigrations);
        Assert.Contains("20260802102357_HardenConcurrencyAndInboxLeases", appliedMigrations);

        await dbContext.Database.OpenConnectionAsync();
        try
        {
            await using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT extversion FROM pg_extension WHERE extname = 'vector';";
            var vectorVersion = await command.ExecuteScalarAsync();
            Assert.NotNull(vectorVersion);

            command.CommandText = "SELECT to_regclass('public.qrtz_job_details')::text;";
            var quartzTable = await command.ExecuteScalarAsync();
            Assert.Equal("qrtz_job_details", quartzTable?.ToString());
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }
}
