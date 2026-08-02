namespace TaskPilot.IntegrationTests;

[Collection(TaskPilotIntegrationTestCollection.Name)]
public sealed class HealthEndpointTests(TaskPilotIntegrationEnvironment environment)
{
    [Fact]
    public async Task Live_health_endpoint_returns_success()
    {
        using var client = environment.Factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Ready_health_endpoint_and_container_protocol_probes_return_success()
    {
        using var client = environment.Factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        var probes = await environment.ProbeInfrastructureAsync();

        response.EnsureSuccessStatusCode();
        Assert.All(probes, probe =>
            Assert.True(
                probe.ExitCode == 0,
                $"{probe.Name} readiness probe failed. stdout={probe.Stdout} stderr={probe.Stderr}"));
        Assert.Contains(probes, probe => probe.Name == "redis" && probe.Stdout.Contains("PONG", StringComparison.Ordinal));
    }
}
