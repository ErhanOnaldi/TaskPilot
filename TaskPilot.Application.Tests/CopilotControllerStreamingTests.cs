using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskPilot.API.Features.Copilot;
using TaskPilot.Application;
using TaskPilot.Application.Features.Copilot;

namespace TaskPilot.Application.Tests;

public sealed class CopilotControllerStreamingTests
{
    [Fact]
    public async Task Stream_endpoint_writes_each_token_as_an_SSE_event_and_finishes_with_completed_event()
    {
        var responseBody = new MemoryStream();
        var controller = new CopilotController(new FakeCopilotService())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    Response = { Body = responseBody }
                }
            }
        };

        var action = await controller.StreamProject(
            12,
            new CopilotRequest("question"),
            CancellationToken.None);

        Assert.IsType<EmptyResult>(action);
        Assert.Equal("text/event-stream", controller.Response.ContentType);
        responseBody.Position = 0;
        var payload = await new StreamReader(responseBody).ReadToEndAsync();
        Assert.Contains("event: session\ndata: {\"sessionId\":91}\n\n", payload);
        Assert.Contains("event: token\ndata: {\"token\":\"hel\"}\n\n", payload);
        Assert.Contains("event: token\ndata: {\"token\":\"lo\"}\n\n", payload);
        Assert.Contains("event: completed\n", payload);
    }

    private sealed class FakeCopilotService : ICopilotService
    {
        public Task<ServiceResult<CopilotStreamResponse>> StreamProjectAsync(
            int projectId,
            CopilotRequest request,
            string? correlationId,
            CancellationToken cancellationToken) =>
            Task.FromResult(ServiceResult<CopilotStreamResponse>.Success(
                new CopilotStreamResponse(91, Updates())));

        public Task<ServiceResult<CopilotStreamResponse>> StreamWorkspaceAsync(int workspaceId, CopilotRequest request, string? correlationId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ServiceResult<CopilotSessionResponse>> ChatProjectAsync(int projectId, CopilotRequest request, string? correlationId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ServiceResult<CopilotSessionResponse>> ChatWorkspaceAsync(int workspaceId, CopilotRequest request, string? correlationId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ServiceResult<CopilotSessionResponse>> GetSessionAsync(int sessionId, CancellationToken cancellationToken) => throw new NotSupportedException();

        private static async IAsyncEnumerable<CopilotStreamUpdate> Updates()
        {
            yield return new CopilotStreamUpdate(CopilotStreamUpdateKind.Token, "hel");
            await Task.Yield();
            yield return new CopilotStreamUpdate(CopilotStreamUpdateKind.Token, "lo");
            yield return new CopilotStreamUpdate(
                CopilotStreamUpdateKind.Completed,
                Session: new CopilotSessionResponse(91, 5, 12, []));
        }
    }
}
