using System.Net;
using System.Runtime.CompilerServices;
using TaskPilot.Application.Features.Ai;
using TaskPilot.Application.Features.Copilot;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.AI;
using TaskPilot.Domain.AI.Copilot;

namespace TaskPilot.Application.Tests;

public sealed class CopilotStreamingTests
{
    [Fact]
    public async Task Streaming_emits_provider_tokens_then_persists_one_cited_assistant_message_on_completion()
    {
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = new CopilotService(
            repository,
            new FakeAuthorization(),
            new FakeRetriever(),
            new FakeStreamingGenerator(),
            unitOfWork,
            new FakeClock());

        var started = await service.StreamProjectAsync(
            12,
            new CopilotRequest("Which task is risky?"),
            "74a94701-5b14-4ed7-b8c2-9eb6fda33102",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, started.Status);
        Assert.NotNull(started.Data);
        Assert.DoesNotContain(repository.Messages, message => message.Role == CopilotMessageRole.Assistant);

        var updates = new List<CopilotStreamUpdate>();
        await foreach (var update in started.Data.ReadUpdatesAsync(CancellationToken.None))
            updates.Add(update);

        Assert.Equal(["Risk ", "is high [Task:42]"], updates.Where(x => x.Kind == CopilotStreamUpdateKind.Token).Select(x => x.Token));
        var completed = Assert.Single(updates, x => x.Kind == CopilotStreamUpdateKind.Completed);
        Assert.NotNull(completed.Session);
        var assistant = Assert.Single(repository.Messages, message => message.Role == CopilotMessageRole.Assistant);
        Assert.Equal("Risk is high [Task:42]", assistant.Content);
        Assert.Contains("\"SourceId\":42", assistant.CitationsJson);
        Assert.Equal(2, unitOfWork.SaveCount); // new session identity + one atomic completion save
    }

    [Fact]
    public async Task Streaming_response_can_only_be_consumed_once_to_prevent_duplicate_persistence()
    {
        var service = new CopilotService(
            new FakeRepository(),
            new FakeAuthorization(),
            new FakeRetriever(),
            new FakeStreamingGenerator(),
            new FakeUnitOfWork(),
            new FakeClock());
        var started = await service.StreamProjectAsync(12, new CopilotRequest("question"), null, CancellationToken.None);

        await foreach (var _ in started.Data!.ReadUpdatesAsync(CancellationToken.None)) { }

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in started.Data.ReadUpdatesAsync(CancellationToken.None)) { }
        });
    }

    private sealed class FakeRepository : ICopilotRepository
    {
        private readonly Dictionary<int, CopilotChatSession> _sessions = [];
        public List<CopilotChatMessage> Messages { get; } = [];

        public Task<CopilotChatSession?> GetSessionAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(_sessions.GetValueOrDefault(id));

        public ValueTask AddSessionAsync(CopilotChatSession session, CancellationToken cancellationToken)
        {
            session.Id = 91;
            _sessions[session.Id] = session;
            return ValueTask.CompletedTask;
        }

        public ValueTask AddMessageAsync(CopilotChatMessage message, CancellationToken cancellationToken)
        {
            message.Id = Messages.Count + 1;
            Messages.Add(message);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeAuthorization : ICopilotAuthorizationPort
    {
        public Task<CopilotAuthorizedScope?> AuthorizeProjectAsync(int projectId, string? correlationId, Guid? causationId, CancellationToken cancellationToken) =>
            Task.FromResult<CopilotAuthorizedScope?>(new(
                new AgentExecutionScope(7, 5, projectId, Guid.Parse(correlationId ?? "74a94701-5b14-4ed7-b8c2-9eb6fda33102"), causationId ?? Guid.NewGuid()),
                new HashSet<int> { projectId },
                false));

        public Task<CopilotAuthorizedScope?> AuthorizeWorkspaceAsync(int workspaceId, string? correlationId, Guid? causationId, CancellationToken cancellationToken) =>
            Task.FromResult<CopilotAuthorizedScope?>(null);
    }

    private sealed class FakeRetriever : ICopilotContextRetriever
    {
        public Task<IReadOnlyList<CopilotContextItem>> RetrieveAsync(CopilotAuthorizedScope scope, string query, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CopilotContextItem>>([
                new(new CopilotCitation(CopilotCitationType.Task, 42), "Task 42 is overdue and blocked.")
            ]);
    }

    private sealed class FakeStreamingGenerator : IAiChatGenerator
    {
        public Task<string> CompleteAsync(IReadOnlyList<AiChatMessage> messages, AgentExecutionScope scope, CancellationToken cancellationToken) =>
            Task.FromResult("Risk is high [Task:42]");

        public async IAsyncEnumerable<string> StreamAsync(
            IReadOnlyList<AiChatMessage> messages,
            AgentExecutionScope scope,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return "Risk ";
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            yield return "is high [Task:42]";
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        private DateTime _now = new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);
        public DateTime UtcNow => _now = _now.AddSeconds(1);
    }
}
