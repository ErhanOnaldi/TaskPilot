using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;

namespace TaskPilot.IntegrationTests;

[Collection(TaskPilotIntegrationTestCollection.Name)]
public sealed class McpEndpointTests(TaskPilotIntegrationEnvironment environment)
{
    [Fact]
    public async Task Official_client_rejects_unauthorized_discovery()
    {
        using var httpClient = environment.Factory.CreateClient();
        await using var transport = CreateTransport(httpClient);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => McpClient.CreateAsync(
            transport,
            clientOptions: null,
            loggerFactory: NullLoggerFactory.Instance,
            cancellationToken: CancellationToken.None));

        Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
    }

    [Fact]
    public async Task Official_client_discovers_server_and_lists_only_authorized_tools()
    {
        using var user = await IntegrationTestApi.RegisterAsync(environment.Factory);
        await using var transport = CreateTransport(user.Client);
        await using var client = await McpClient.CreateAsync(
            transport,
            clientOptions: null,
            loggerFactory: NullLoggerFactory.Instance,
            cancellationToken: CancellationToken.None);

        var tools = await client.ListToolsAsync(cancellationToken: CancellationToken.None);

        Assert.NotNull(client.ServerCapabilities.Tools);
        Assert.False(string.IsNullOrWhiteSpace(client.ServerInfo.Name));
        Assert.Equal(
            ["notes.read", "projects.list", "semantic.search", "tasks.create", "tasks.list"],
            tools.Select(tool => tool.Name).Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public async Task Official_client_creates_task_visible_through_normal_api()
    {
        using var user = await IntegrationTestApi.RegisterAsync(environment.Factory);
        var workspace = await IntegrationTestApi.CreateWorkspaceAsync(user.Client, "McpWorkspace");
        var project = await IntegrationTestApi.CreateProjectAsync(user.Client, workspace.Id, "McpProject");
        var title = $"MCP-created-{Guid.NewGuid():N}";

        await using var transport = CreateTransport(user.Client);
        await using var client = await McpClient.CreateAsync(
            transport,
            clientOptions: null,
            loggerFactory: NullLoggerFactory.Instance,
            cancellationToken: CancellationToken.None);

        var result = await client.CallToolAsync(
            "tasks.create",
            new Dictionary<string, object?>
            {
                ["projectId"] = project.Id,
                ["title"] = title,
                ["description"] = "Created through the official MCP client."
            },
            cancellationToken: CancellationToken.None);

        Assert.NotEqual(true, result.IsError);
        using var apiResponse = await user.Client.GetAsync($"/api/projects/{project.Id}/tasks");
        apiResponse.EnsureSuccessStatusCode();
        Assert.Contains(title, await apiResponse.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    private HttpClientTransport CreateTransport(HttpClient httpClient) => new(
        new HttpClientTransportOptions
        {
            Endpoint = new Uri(httpClient.BaseAddress!, "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            Name = "TaskPilot integration test client"
        },
        httpClient,
        NullLoggerFactory.Instance,
        ownsHttpClient: false);
}
