using System.Net;
using A2A;
using TaskPilot.Application.Features.Interop;

namespace TaskPilot.IntegrationTests;

[Collection(TaskPilotIntegrationTestCollection.Name)]
public sealed class A2aExternalClientAcceptanceTests(TaskPilotIntegrationEnvironment environment)
{
    [Fact]
    public async Task External_a2a_client_discovers_the_published_agent_card_without_a_token()
    {
        using var anonymousClient = environment.Factory.CreateClient();

        var (_, card) = await ExternalA2aCopilotClient.DiscoverAsync(anonymousClient);

        Assert.Equal("TaskPilot Copilot", card.Name);
        Assert.Contains(card.SupportedInterfaces, endpoint => endpoint.Url == "http://localhost/a2a");
    }

    [Fact]
    public async Task External_a2a_client_requires_a_jwt_before_sending_a_message()
    {
        using var anonymousClient = environment.Factory.CreateClient();
        var (client, _) = await ExternalA2aCopilotClient.DiscoverAsync(anonymousClient);

        var exception = await Assert.ThrowsAsync<A2AException>(() => client.RunAsync(
            new A2aMessageRequest(1, 1, "unauthorized-message"),
            Guid.NewGuid().ToString("N")));

        Assert.Contains(((int)HttpStatusCode.Unauthorized).ToString(), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task External_a2a_client_continues_a_jwt_scoped_session_without_leaking_it_to_another_jwt()
    {
        using var firstUser = await IntegrationTestApi.RegisterAsync(environment.Factory);
        using var secondUser = await IntegrationTestApi.RegisterAsync(environment.Factory);
        var firstWorkspace = await IntegrationTestApi.CreateWorkspaceAsync(firstUser.Client, "First-A2A");
        var firstProject = await IntegrationTestApi.CreateProjectAsync(firstUser.Client, firstWorkspace.Id, "First-A2A");
        var secondWorkspace = await IntegrationTestApi.CreateWorkspaceAsync(secondUser.Client, "Second-A2A");
        var secondProject = await IntegrationTestApi.CreateProjectAsync(secondUser.Client, secondWorkspace.Id, "Second-A2A");
        var (firstClient, _) = await ExternalA2aCopilotClient.DiscoverAsync(firstUser.Client);
        var (secondClient, _) = await ExternalA2aCopilotClient.DiscoverAsync(secondUser.Client);
        var sharedContextId = Guid.NewGuid().ToString("N");

        var firstRun = await firstClient.RunAsync(
            new A2aMessageRequest(firstWorkspace.Id, firstProject.Id, "first-user-turn"),
            sharedContextId);
        var continuation = await firstClient.RunAsync(
            new A2aMessageRequest(firstWorkspace.Id, firstProject.Id, "first-user-continuation"),
            sharedContextId);
        var isolatedRun = await secondClient.RunAsync(
            new A2aMessageRequest(secondWorkspace.Id, secondProject.Id, "second-user-turn"),
            sharedContextId);

        Assert.Equal(sharedContextId, firstRun.ContextId);
        Assert.Contains("history=first-user-turn", firstRun.Text, StringComparison.Ordinal);
        Assert.Contains("first-user-turn", continuation.Text, StringComparison.Ordinal);
        Assert.Contains("first-user-continuation", continuation.Text, StringComparison.Ordinal);
        Assert.Contains($"scope=user:{firstUser.Auth.User.Id};workspace:{firstWorkspace.Id};project:{firstProject.Id}", continuation.Text, StringComparison.Ordinal);
        Assert.Contains("history=second-user-turn", isolatedRun.Text, StringComparison.Ordinal);
        Assert.Contains($"scope=user:{secondUser.Auth.User.Id};workspace:{secondWorkspace.Id};project:{secondProject.Id}", isolatedRun.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("first-user-turn", isolatedRun.Text, StringComparison.Ordinal);
        Assert.DoesNotContain($"scope=user:{firstUser.Auth.User.Id}", isolatedRun.Text, StringComparison.Ordinal);
    }
}
