using System.Net;

namespace TaskPilot.IntegrationTests;

[Collection(TaskPilotIntegrationTestCollection.Name)]
public sealed class AuthorizationEndpointTests(TaskPilotIntegrationEnvironment environment)
{
    [Fact]
    public async Task Protected_project_endpoint_rejects_anonymous_requests()
    {
        using var client = environment.Factory.CreateClient();

        using var response = await client.GetAsync("/api/projects/1");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_cannot_read_another_workspaces_project_or_task()
    {
        using var firstUser = await IntegrationTestApi.RegisterAsync(environment.Factory);
        using var secondUser = await IntegrationTestApi.RegisterAsync(environment.Factory);
        var firstWorkspace = await IntegrationTestApi.CreateWorkspaceAsync(firstUser.Client, "First");
        var firstProject = await IntegrationTestApi.CreateProjectAsync(firstUser.Client, firstWorkspace.Id, "First");
        var secondWorkspace = await IntegrationTestApi.CreateWorkspaceAsync(secondUser.Client, "Second");
        var secondProject = await IntegrationTestApi.CreateProjectAsync(secondUser.Client, secondWorkspace.Id, "Second");
        var secondTask = await IntegrationTestApi.CreateTaskAsync(secondUser.Client, secondProject.Id, "Second");

        using var ownProjectResponse = await firstUser.Client.GetAsync($"/api/projects/{firstProject.Id}");
        using var foreignWorkspaceResponse = await firstUser.Client.GetAsync($"/api/workspaces/{secondWorkspace.Id}/projects");
        using var foreignProjectResponse = await firstUser.Client.GetAsync($"/api/projects/{secondProject.Id}");
        using var foreignTaskResponse = await firstUser.Client.GetAsync($"/api/tasks/{secondTask.Id}");

        Assert.Equal(HttpStatusCode.OK, ownProjectResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreignWorkspaceResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreignProjectResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, foreignTaskResponse.StatusCode);
    }
}
