using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Infrastructure.Messaging;
using TaskPilot.Persistence;
using TaskPilot.Persistence.Messaging;

namespace TaskPilot.IntegrationTests;

[Collection(TaskPilotIntegrationTestCollection.Name)]
public sealed class DashboardInvalidationTests(TaskPilotIntegrationEnvironment environment)
{
    [Fact]
    public async Task Task_creation_eventually_invalidates_cached_dashboard_after_outbox_dispatch()
    {
        using var owner = await IntegrationTestApi.RegisterAsync(environment.Factory);
        var workspace = await IntegrationTestApi.CreateWorkspaceAsync(owner.Client, "Dashboard");
        var project = await IntegrationTestApi.CreateProjectAsync(owner.Client, workspace.Id, "Dashboard");
        var cachedDashboard = await IntegrationTestApi.GetDashboardAsync(owner.Client, project.Id);
        Assert.Equal(0, cachedDashboard.TotalTasks);

        _ = await IntegrationTestApi.CreateTaskAsync(owner.Client, project.Id, "Dashboard");
        var stillCached = await IntegrationTestApi.GetDashboardAsync(owner.Client, project.Id);
        Assert.Equal(0, stillCached.TotalTasks);

        await using (var scope = environment.Factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.True(await dbContext.Set<OutboxMessage>().AnyAsync(message =>
                message.EventType == "project.dashboard-invalidation.requested" &&
                message.DispatchedAtUtc == null));

            var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();
            Assert.True(await dispatcher.DispatchPendingAsync(500, CancellationToken.None) >= 1);
        }

        var refreshed = await Eventually.UntilAsync(
            () => IntegrationTestApi.GetDashboardAsync(owner.Client, project.Id),
            dashboard => dashboard.TotalTasks == 1,
            "the RabbitMQ invalidation consumer to evict the Redis dashboard cache");

        Assert.Equal(1, refreshed.TodoTasks);
        Assert.Equal(1, refreshed.UnassignedTasks);
    }
}
