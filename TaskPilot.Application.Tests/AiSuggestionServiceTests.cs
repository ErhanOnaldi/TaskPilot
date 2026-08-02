using System.Net;
using TaskPilot.Application;
using TaskPilot.Application.Authorization.Abstractions;
using TaskPilot.Application.Authorization.Enums;
using TaskPilot.Application.Authorization.Results;
using TaskPilot.Application.Features.Ai;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Infrastructure.Messaging;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.AI;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Application.Tests;

public sealed class AiSuggestionServiceTests
{
    [Fact]
    public async Task Create_persists_pending_request_enqueues_event_and_returns_accepted_without_calling_provider()
    {
        var fixture = new Fixture();
        var correlationId = Guid.NewGuid();

        var result = await fixture.Service.CreateAsync(
            12,
            new CreateTaskSuggestionRequest("Prepare release notes"),
            correlationId,
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Accepted, result.Status);
        Assert.Equal(AiSuggestionStatus.Pending, result.Data!.Status);
        Assert.Equal(0, fixture.Generator.CallCount);
        var requested = Assert.IsType<AiSuggestionRequestedEvent>(fixture.Outbox.Events.Single());
        Assert.Equal(result.Data.Id, requested.SuggestionId);
        Assert.Equal(correlationId, requested.CorrelationId);
        Assert.Equal(7, requested.UserId);
        Assert.Equal(5, requested.WorkspaceId);
        Assert.Equal(12, requested.ProjectId);
    }

    [Fact]
    public async Task Processor_uses_event_scope_real_context_and_constrains_labels_to_project_labels()
    {
        var fixture = new Fixture();
        fixture.Generator.Suggestion = new AiTaskSuggestion(
            "Release notes", "High", ["RELEASE", "invented"], ["Draft", "Review"], null);
        var pending = await fixture.CreatePendingAsync();

        await fixture.ProcessRequestedAsync();

        var completed = await fixture.Service.GetAsync(pending.Id, CancellationToken.None);
        Assert.Equal(AiSuggestionStatus.Completed, completed.Data!.Status);
        Assert.Equal(["release"], completed.Data.Labels);
        Assert.Equal(fixture.Context.Context, fixture.Generator.LastContext);
        Assert.Equal(1, fixture.Suggestions.NotificationCount);
        Assert.Equal("AiSuggestionCompleted", fixture.Suggestions.LastNotificationType);
        var requested = fixture.RequestedEvent;
        Assert.Equal(requested.EventId, fixture.Generator.LastScope!.CausationId);
        Assert.Equal(requested.CorrelationId, fixture.Generator.LastScope.CorrelationId);
    }

    [Fact]
    public async Task Processor_revalidates_membership_and_fails_without_calling_provider_when_membership_was_removed()
    {
        var fixture = new Fixture();
        var pending = await fixture.CreatePendingAsync();
        fixture.Suggestions.HasCurrentMembership = false;

        await fixture.ProcessRequestedAsync();

        Assert.Equal(AiSuggestionStatus.Failed.ToString(), fixture.Suggestions.GetRequired(pending.Id).Status);
        Assert.Equal(0, fixture.Generator.CallCount);
        Assert.Equal(1, fixture.Suggestions.NotificationCount);
        Assert.Equal("AiSuggestionFailed", fixture.Suggestions.LastNotificationType);
    }

    [Fact]
    public async Task Duplicate_delivery_is_idempotent_after_terminal_state()
    {
        var fixture = new Fixture();
        await fixture.CreatePendingAsync();

        await fixture.ProcessRequestedAsync();
        await fixture.ProcessRequestedAsync();

        Assert.Equal(1, fixture.Generator.CallCount);
        Assert.Equal(1, fixture.Suggestions.NotificationCount);
        Assert.Single(fixture.Outbox.Events.OfType<AiSuggestionCompletedEvent>());
    }

    [Fact]
    public async Task Provider_exception_is_mapped_to_failed_without_exposing_details()
    {
        var fixture = new Fixture();
        fixture.Generator.Exception = new InvalidOperationException("provider-secret-and-endpoint");
        var pending = await fixture.CreatePendingAsync();

        await fixture.ProcessRequestedAsync();

        var entity = fixture.Suggestions.GetRequired(pending.Id);
        Assert.Equal(AiSuggestionStatus.Failed.ToString(), entity.Status);
        Assert.DoesNotContain("provider-secret", entity.ErrorMessage ?? string.Empty);
        Assert.Equal("AI generation could not be completed.", entity.ErrorMessage);
        Assert.Same(fixture.Generator.Exception, fixture.Telemetry.Exception);
    }

    [Fact]
    public async Task Completed_suggestion_applies_once_and_returns_same_applied_result()
    {
        var fixture = new Fixture();
        var created = await fixture.CreateCompletedAsync();

        var firstApply = await fixture.Service.ApplyAsync(created.Id, CancellationToken.None);
        var secondApply = await fixture.Service.ApplyAsync(created.Id, CancellationToken.None);

        Assert.Equal(AiSuggestionStatus.Applied, firstApply.Data!.Status);
        Assert.Equal(firstApply.Data.AppliedTaskId, secondApply.Data!.AppliedTaskId);
        Assert.Equal(1, fixture.ApplyPort.CreatedTaskCount);
    }

    [Fact]
    public async Task Concurrent_apply_attempts_use_the_single_apply_port_claim()
    {
        var fixture = new Fixture { SynchronizeApplyAttempts = true };
        var created = await fixture.CreateCompletedAsync();

        var results = await Task.WhenAll(
            fixture.Service.ApplyAsync(created.Id, CancellationToken.None),
            fixture.Service.ApplyAsync(created.Id, CancellationToken.None));

        Assert.All(results, result => Assert.Equal(AiSuggestionStatus.Applied, result.Data!.Status));
        Assert.Equal(1, fixture.ApplyPort.CreatedTaskCount);
        Assert.Single(results.Select(result => result.Data!.AppliedTaskId).Distinct());
    }

    [Fact]
    public async Task Requestor_only_scope_blocks_another_current_member()
    {
        var fixture = new Fixture();
        var pending = await fixture.CreatePendingAsync();
        fixture.Access.CurrentUserId = 8;

        var result = await fixture.Service.GetAsync(pending.Id, CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.Forbidden, result.Status);
    }

    [Fact]
    public async Task Apply_revalidates_membership_inside_the_atomic_apply_port()
    {
        var fixture = new Fixture();
        var completed = await fixture.CreateCompletedAsync();
        fixture.ApplyPort.HasCurrentMembership = false;

        var result = await fixture.Service.ApplyAsync(completed.Id, CancellationToken.None);

        Assert.True(result.IsFail);
        Assert.Equal(HttpStatusCode.Forbidden, result.Status);
        Assert.Equal(0, fixture.ApplyPort.CreatedTaskCount);
    }

    private sealed class Fixture
    {
        public readonly FakeSuggestions Suggestions = new();
        public readonly FakeAccess Access = new();
        public readonly FakeGenerator Generator = new();
        public readonly FakeOutbox Outbox = new();
        public readonly FakeContextReader Context = new();
        public readonly FakeTelemetry Telemetry = new();
        public readonly FakeApplyPort ApplyPort;
        public readonly AiSuggestionService Service;
        public readonly AiSuggestionProcessor Processor;

        public bool SynchronizeApplyAttempts
        {
            set => ApplyPort.SynchronizeAttempts = value;
        }

        public AiSuggestionRequestedEvent RequestedEvent => Assert.IsType<AiSuggestionRequestedEvent>(Outbox.Events.First());

        public Fixture()
        {
            var clock = new FakeClock();
            var inputGuard = new AiInputGuard();
            var outputGuard = new AiOutputGuard();
            ApplyPort = new FakeApplyPort(Suggestions);
            Service = new AiSuggestionService(
                Suggestions,
                ApplyPort,
                new FakeUnitOfWork(),
                Access,
                inputGuard,
                outputGuard,
                clock,
                Outbox);
            Processor = new AiSuggestionProcessor(Suggestions, Context, Generator, outputGuard, Outbox, clock, Telemetry);
        }

        public async Task<AiSuggestionResponse> CreatePendingAsync()
        {
            var result = await Service.CreateAsync(
                12,
                new CreateTaskSuggestionRequest("Prepare release notes"),
                Guid.NewGuid(),
                CancellationToken.None);
            return result.Data!;
        }

        public Task ProcessRequestedAsync()
        {
            var requested = RequestedEvent;
            var scope = new AgentExecutionScope(
                requested.UserId,
                requested.WorkspaceId,
                requested.ProjectId,
                requested.CorrelationId,
                requested.EventId);
            return Processor.ProcessAsync(requested.SuggestionId, scope, CancellationToken.None);
        }

        public async Task<AiSuggestionResponse> CreateCompletedAsync()
        {
            var pending = await CreatePendingAsync();
            await ProcessRequestedAsync();
            return (await Service.GetAsync(pending.Id, CancellationToken.None)).Data!;
        }
    }

    private sealed class FakeSuggestions : IAiSuggestionRepository
    {
        private readonly Dictionary<int, AiSuggestion> _items = [];
        private readonly object _gate = new();
        private int _nextId;
        public bool HasCurrentMembership { get; set; } = true;
        public int NotificationCount { get; private set; }
        public string? LastNotificationType { get; private set; }

        public Task<AiSuggestion?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(_items.GetValueOrDefault(id));

        public ValueTask AddAsync(AiSuggestion suggestion, CancellationToken cancellationToken)
        {
            suggestion.Id = ++_nextId;
            _items.Add(suggestion.Id, suggestion);
            return ValueTask.CompletedTask;
        }

        public Task<int> CountRequestedByUserOnUtcDateAsync(int userId, DateOnly utcDate, CancellationToken cancellationToken) =>
            Task.FromResult(0);

        public Task<AiSuggestionClaimResult> ClaimForProcessingAsync(int suggestionId, AgentExecutionScope scope, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (!_items.TryGetValue(suggestionId, out var entity))
                    return Task.FromResult(new AiSuggestionClaimResult(AiSuggestionClaimDisposition.NotFound, null));
                if (entity.RequestedByUserId != scope.UserId || entity.ProjectId != scope.ProjectId)
                    return Task.FromResult(new AiSuggestionClaimResult(AiSuggestionClaimDisposition.ScopeMismatch, null));
                if (entity.Status != AiSuggestionStatus.Pending.ToString())
                    return Task.FromResult(new AiSuggestionClaimResult(AiSuggestionClaimDisposition.AlreadyHandled, entity));
                entity.Status = AiSuggestionStatus.Processing.ToString();
                return Task.FromResult(new AiSuggestionClaimResult(
                    HasCurrentMembership ? AiSuggestionClaimDisposition.Claimed : AiSuggestionClaimDisposition.MembershipRequired,
                    entity));
            }
        }

        public Task<bool> CompleteProcessingAsync(int suggestionId, int requestingUserId, Guid notificationSourceEventId, AiTaskSuggestion suggestion, AiGenerationMetadata metadata, DateTime completedAtUtc, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var entity = _items[suggestionId];
                if (entity.RequestedByUserId != requestingUserId || entity.Status != AiSuggestionStatus.Processing.ToString())
                    return Task.FromResult(false);
                entity.SuggestedPriority = suggestion.Priority;
                entity.SuggestedLabels = System.Text.Json.JsonSerializer.Serialize(suggestion.Labels);
                entity.SuggestedDueDate = suggestion.DueDate;
                entity.SuggestedSubtasks = AiSuggestionMetadataCodec.Serialize(suggestion, metadata);
                entity.Status = AiSuggestionStatus.Completed.ToString();
                entity.CompletedAt = completedAtUtc;
                NotificationCount++;
                LastNotificationType = "AiSuggestionCompleted";
                return Task.FromResult(true);
            }
        }

        public Task<bool> FailProcessingAsync(int suggestionId, int requestingUserId, Guid notificationSourceEventId, string publicErrorMessage, DateTime completedAtUtc, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                var entity = _items[suggestionId];
                if (entity.RequestedByUserId != requestingUserId || entity.Status != AiSuggestionStatus.Processing.ToString())
                    return Task.FromResult(false);
                entity.Status = AiSuggestionStatus.Failed.ToString();
                entity.ErrorMessage = publicErrorMessage;
                entity.CompletedAt = completedAtUtc;
                NotificationCount++;
                LastNotificationType = "AiSuggestionFailed";
                return Task.FromResult(true);
            }
        }

        public AiSuggestion GetRequired(int id) => _items[id];
    }

    private sealed class FakeApplyPort(FakeSuggestions suggestions) : IAiSuggestionApplyPort
    {
        private readonly object _gate = new();
        private readonly TaskCompletionSource _bothAttempts = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _attemptCount;
        public bool HasCurrentMembership { get; set; } = true;
        public bool SynchronizeAttempts { get; set; }
        public int CreatedTaskCount { get; private set; }

        public async Task<AiSuggestionApplyResult> ApplyOnceAsync(int suggestionId, int requestingUserId, AiTaskSuggestion suggestion, AiGenerationMetadata metadata, DateTime appliedAtUtc, CancellationToken cancellationToken)
        {
            if (SynchronizeAttempts)
            {
                if (Interlocked.Increment(ref _attemptCount) == 2) _bothAttempts.TrySetResult();
                await _bothAttempts.Task.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken);
            }

            lock (_gate)
            {
                if (!HasCurrentMembership)
                    return new AiSuggestionApplyResult(AiSuggestionApplyDisposition.MembershipRequired, null);
                var entity = suggestions.GetRequired(suggestionId);
                if (entity.Status == AiSuggestionStatus.Applied.ToString())
                    return new AiSuggestionApplyResult(AiSuggestionApplyDisposition.AlreadyApplied, entity);
                if (entity.Status != AiSuggestionStatus.Completed.ToString() || entity.RequestedByUserId != requestingUserId)
                    return new AiSuggestionApplyResult(AiSuggestionApplyDisposition.NotReady, entity);

                CreatedTaskCount++;
                entity.TaskId = 100 + CreatedTaskCount;
                entity.Status = AiSuggestionStatus.Applied.ToString();
                entity.SuggestedSubtasks = AiSuggestionMetadataCodec.Serialize(
                    suggestion,
                    metadata with { AppliedAtUtc = appliedAtUtc, AppliedTaskId = entity.TaskId });
                return new AiSuggestionApplyResult(AiSuggestionApplyDisposition.Applied, entity);
            }
        }
    }

    private sealed class FakeOutbox : IEventOutbox
    {
        private readonly List<Func<IIntegrationEvent>> _factories = [];
        public IReadOnlyList<IIntegrationEvent> Events => _factories.Select(factory => factory()).ToArray();
        public Task EnqueueAsync(Func<IIntegrationEvent> eventFactory, CancellationToken cancellationToken)
        {
            _factories.Add(eventFactory);
            return Task.CompletedTask;
        }
        public Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeContextReader : IProjectAiContextReader
    {
        public ProjectAiContext Context { get; } = new(
            new HashSet<string>(["release", "backend"], StringComparer.OrdinalIgnoreCase),
            ["member@example.test"],
            ["Ship version"]);
        public Task<ProjectAiContext> ReadAsync(AgentExecutionScope scope, CancellationToken cancellationToken) =>
            Task.FromResult(Context);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTime UtcNow => new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeGenerator : IAiStructuredGenerator
    {
        public AgentExecutionScope? LastScope { get; private set; }
        public ProjectAiContext? LastContext { get; private set; }
        public Exception? Exception { get; set; }
        public int CallCount { get; private set; }
        public AiTaskSuggestion Suggestion { get; set; } =
            new("Release notes", "High", ["release"], ["Draft", "Review"], null);

        public Task<AiGenerationResult> GenerateTaskSuggestionAsync(string input, AgentExecutionScope scope, CancellationToken cancellationToken) =>
            GenerateTaskSuggestionAsync(input, scope, null, cancellationToken);

        public Task<AiGenerationResult> GenerateTaskSuggestionAsync(string input, AgentExecutionScope scope, ProjectAiContext? context, CancellationToken cancellationToken)
        {
            CallCount++;
            LastScope = scope;
            LastContext = context;
            return Exception is not null
                ? Task.FromException<AiGenerationResult>(Exception)
                : Task.FromResult(new AiGenerationResult(
                    Suggestion,
                    new AiGenerationMetadata("fake", "test", "v1", 5, 10, 1, scope.CausationId)));
        }
    }

    private sealed class FakeTelemetry : IAiRunTelemetry
    {
        public Exception? Exception { get; private set; }
        public string? RejectionReason { get; private set; }

        public void GenerationFailed(AgentExecutionScope scope, Exception exception) => Exception = exception;
        public void OutputRejected(AgentExecutionScope scope, string reason) => RejectionReason = reason;
    }

    private sealed class FakeAccess : IAccessControlService
    {
        public int CurrentUserId { get; set; } = 7;
        public Task<WorkspaceAccessResult> AuthorizeWorkspaceAsync(int workspaceId, WorkspaceAccessLevel accessLevel, bool requireActiveWorkspace, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<ProjectAccessResult> AuthorizeProjectAsync(int projectId, ProjectAccessLevel accessLevel, bool requireActiveProject, CancellationToken cancellationToken) =>
            Task.FromResult(new ProjectAccessResult(
                new Project { Id = projectId, WorkspaceId = 5 },
                new WorkSpace { Id = 5 },
                new WorkspaceMember { UserId = CurrentUserId },
                CurrentUserId,
                null,
                new ProjectMember { ProjectId = projectId, UserId = CurrentUserId }));
    }
}
