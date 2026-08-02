using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskPilot.Application;
using TaskPilot.Application.Features.Auth.Dtos;
using TaskPilot.Application.Features.Dashboard.Dtos;
using TaskPilot.Application.Features.Project.Dtos;
using TaskPilot.Application.Features.Tasks.Dtos;
using TaskPilot.Application.Features.Workspace.Dtos;

namespace TaskPilot.IntegrationTests;

internal static class IntegrationTestApi
{
    private const string Password = "Integration-Password-42!";

    public static async Task<RegisteredClient> RegisterAsync(TaskPilotWebApplicationFactory factory)
    {
        var client = factory.CreateClient();
        var email = $"integration-{Guid.NewGuid():N}@taskpilot.test";
        using var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, Password));
        var auth = await ReadDataAsync<AuthResponse>(response);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return new RegisteredClient(client, auth);
    }

    public static async Task<WorkspaceResponse> CreateWorkspaceAsync(HttpClient client, string prefix = "Workspace")
    {
        using var response = await client.PostAsJsonAsync(
            "/api/workspaces",
            new CreateWorkspaceRequest($"{prefix}-{Guid.NewGuid():N}"));
        return await ReadDataAsync<WorkspaceResponse>(response);
    }

    public static async Task<ProjectResponse> CreateProjectAsync(HttpClient client, int workspaceId, string prefix = "Project")
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/workspaces/{workspaceId}/projects",
            new CreateProjectRequest($"{prefix}-{Guid.NewGuid():N}", "Integration test project"));
        return await ReadDataAsync<ProjectResponse>(response);
    }

    public static async Task<TaskResponse> CreateTaskAsync(HttpClient client, int projectId, string prefix = "Task")
    {
        using var response = await client.PostAsJsonAsync(
            $"/api/projects/{projectId}/tasks",
            new CreateTaskRequest($"{prefix}-{Guid.NewGuid():N}", "Integration test task", null, null, null));
        return await ReadDataAsync<TaskResponse>(response);
    }

    public static async Task<ProjectDashboardResponse> GetDashboardAsync(HttpClient client, int projectId)
    {
        using var response = await client.GetAsync($"/api/projects/{projectId}/dashboard");
        return await ReadDataAsync<ProjectDashboardResponse>(response);
    }

    private static async Task<T> ReadDataAsync<T>(HttpResponseMessage response)
        where T : class
    {
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ServiceResult<T>>();
        return result?.Data ?? throw new InvalidOperationException($"The API returned no {typeof(T).Name} data.");
    }
}

internal sealed record RegisteredClient(HttpClient Client, AuthResponse Auth) : IDisposable
{
    public void Dispose() => Client.Dispose();
}
