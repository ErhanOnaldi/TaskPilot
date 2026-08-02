using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace TaskPilot.IntegrationTests;

/// <summary>
/// One isolated infrastructure stack per xUnit collection. Testcontainers maps dynamic host ports,
/// so independently running test processes do not contend for fixed ports.
/// </summary>
public sealed class TaskPilotIntegrationEnvironment : IAsyncLifetime
{
    private readonly string _databaseName = $"taskpilot_it_{Guid.NewGuid():N}";
    private readonly string _userName = "integration";
    private readonly string _password = Guid.NewGuid().ToString("N");
    private readonly PostgreSqlContainer _postgres;
    private readonly RedisContainer _redis;
    private readonly RabbitMqContainer _rabbitMq;

    public TaskPilotIntegrationEnvironment()
    {
        _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg16")
            .WithDatabase(_databaseName)
            .WithUsername(_userName)
            .WithPassword(_password)
            .Build();
        _redis = new RedisBuilder("redis:7.4-alpine")
            .Build();
        _rabbitMq = new RabbitMqBuilder("rabbitmq:4-management-alpine")
            .WithUsername(_userName)
            .WithPassword(_password)
            .Build();
    }

    public TaskPilotWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgres.StartAsync(),
            _redis.StartAsync(),
            _rabbitMq.StartAsync());

        Factory = new TaskPilotWebApplicationFactory(BuildConfigurationOverrides());
    }

    public async Task DisposeAsync()
    {
        Factory?.Dispose();
        await Task.WhenAll(
            _rabbitMq.DisposeAsync().AsTask(),
            _redis.DisposeAsync().AsTask(),
            _postgres.DisposeAsync().AsTask());
    }

    public async Task<IReadOnlyList<InfrastructureProbeResult>> ProbeInfrastructureAsync()
    {
        var postgres = await _postgres.ExecAsync([
            "pg_isready", "--host", "localhost", "--dbname", _databaseName, "--username", _userName
        ]);
        var redis = await _redis.ExecAsync(["redis-cli", "ping"]);
        var rabbitMq = await _rabbitMq.ExecAsync(["rabbitmq-diagnostics", "-q", "ping"]);

        return
        [
            new("postgres", postgres.ExitCode, postgres.Stdout, postgres.Stderr),
            new("redis", redis.ExitCode, redis.Stdout, redis.Stderr),
            new("rabbitmq", rabbitMq.ExitCode, rabbitMq.Stdout, rabbitMq.Stderr)
        ];
    }

    public async Task<bool> IsIntegrationEventQueueIdleAsync()
    {
        var result = await _rabbitMq.ExecAsync([
            "rabbitmqctl", "-q", "list_queues", "name", "messages_ready", "messages_unacknowledged"
        ]);
        if (result.ExitCode != 0)
        {
            return false;
        }

        var queue = result.Stdout
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .SingleOrDefault(columns => columns is ["taskpilot.integration-events", _, _]);

        return queue is [_, "0", "0"];
    }

    private IReadOnlyDictionary<string, string?> BuildConfigurationOverrides()
    {
        var rabbitMqPort = _rabbitMq.GetMappedPublicPort(5672);

        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSql"] = _postgres.GetConnectionString(),
            ["ConnectionStrings:Redis"] = _redis.GetConnectionString(),
            ["RabbitMq:HostName"] = _rabbitMq.Hostname,
            ["RabbitMq:Port"] = rabbitMqPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["RabbitMq:UserName"] = _userName,
            ["RabbitMq:Password"] = _password,
            ["RabbitMq:VirtualHost"] = "/",
            ["Jwt:Issuer"] = "TaskPilot.IntegrationTests",
            ["Jwt:Audience"] = "TaskPilot.IntegrationTests",
            ["Jwt:Secret"] = "integration-tests-only-signing-key-with-32-bytes",
            ["Jwt:AccessTokenExpirationMinutes"] = "15",
            ["Jwt:RefreshTokenExpirationDays"] = "7",
            ["Persistence:ApplyMigrationsOnStartup"] = "true",
            ["Features:Interop:McpEnabled"] = "true",
            ["Features:Interop:A2aEnabled"] = "true",
            ["Features:Interop:PublicBaseUrl"] = "http://localhost"
        };
    }
}

public sealed record InfrastructureProbeResult(string Name, long? ExitCode, string Stdout, string Stderr);
