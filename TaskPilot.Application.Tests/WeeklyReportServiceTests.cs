using System.Net;
using System.Text.Json;
using TaskPilot.Application;
using TaskPilot.Application.Features.Reports;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Infrastructure.Auditing;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.AI;
using TaskPilot.Domain.AI.Reports;

namespace TaskPilot.Application.Tests;

public sealed class WeeklyReportServiceTests
{
    [Fact]
    public async Task Data_collector_turns_the_project_snapshot_into_deterministic_report_data_without_an_agent()
    {
        var collector = new ReportDataCollector(new FixedProjectDataPort(new ReportProjectSnapshot(
            TotalTasks: 8,
            CompletedTasks: 3,
            OverdueTasks: 2,
            UnassignedOpenTasks: 1,
            RecentlyCompletedTitles: ["Release notes"],
            NewCommentCount: 4,
            RecentlyUpdatedAssignedTaskCount: 2)));

        var result = await collector.CollectAsync(
            new AgentExecutionScope(7, 5, 12, Guid.NewGuid(), Guid.NewGuid()),
            new DateOnly(2026, 8, 2),
            CancellationToken.None);

        Assert.Equal("3/8 tasks completed. Recently completed: Release notes. 4 new comments.", result.Progress);
        Assert.Equal("2 overdue tasks.", result.Risks);
        Assert.Equal("1 open tasks are unassigned. 2 assigned tasks changed this week.", result.Workload);
    }

    [Fact]
    public async Task Create_resumes_from_each_completed_business_checkpoint_without_repeating_it()
    {
        var fixture = new Fixture();
        fixture.Reports.Checkpoints["collected"] = new WeeklyReportCheckpoint
        {
            ProjectId = 12,
            SourceEventId = fixture.SourceEventId,
            Stage = "collected",
            PayloadJson = JsonSerializer.Serialize(new ReportData("saved progress", "saved risks", "saved workload"))
        };
        fixture.Reports.Checkpoints["analysis:Progress"] = new WeeklyReportCheckpoint
        {
            ProjectId = 12,
            SourceEventId = fixture.SourceEventId,
            Stage = "analysis:Progress",
            PayloadJson = JsonSerializer.Serialize("saved progress analysis")
        };

        var result = await fixture.Service.CreateAsync(12, fixture.Request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, result.Status);
        Assert.Equal(0, fixture.Collector.CallCount);
        Assert.Equal(0, fixture.Progress.CallCount);
        Assert.Equal(1, fixture.Risk.CallCount);
        Assert.Equal(1, fixture.Workload.CallCount);
        Assert.Contains("saved progress analysis", result.Data!.Content);
        Assert.Contains("risk analysis", result.Data.Content);
        Assert.Contains("workload analysis", result.Data.Content);
        Assert.Equal([1], fixture.Notifications.ReviewReadyReportIds);
        Assert.Equal(["created"], fixture.Audit.Actions);
    }

    [Fact]
    public async Task Create_resumes_from_writer_checkpoint_without_reinvoking_the_writer()
    {
        var fixture = new Fixture();
        fixture.Reports.Checkpoints["collected"] = new WeeklyReportCheckpoint
        {
            ProjectId = 12,
            SourceEventId = fixture.SourceEventId,
            Stage = "collected",
            PayloadJson = JsonSerializer.Serialize(new ReportData("progress", "risks", "workload"))
        };
        foreach (var pair in new Dictionary<string, string>
                 {
                     ["Progress"] = "progress analysis",
                     ["Risk"] = "risk analysis",
                     ["Workload"] = "workload analysis"
                 })
        {
            fixture.Reports.Checkpoints[$"analysis:{pair.Key}"] = new WeeklyReportCheckpoint
            {
                ProjectId = 12,
                SourceEventId = fixture.SourceEventId,
                Stage = $"analysis:{pair.Key}",
                PayloadJson = JsonSerializer.Serialize(pair.Value)
            };
        }
        fixture.Reports.Checkpoints["written"] = new WeeklyReportCheckpoint
        {
            ProjectId = 12,
            SourceEventId = fixture.SourceEventId,
            Stage = "written",
            PayloadJson = JsonSerializer.Serialize("# Resumed weekly report")
        };

        var result = await fixture.Service.CreateAsync(12, fixture.Request, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, result.Status);
        Assert.Equal(0, fixture.Collector.CallCount);
        Assert.Equal(0, fixture.Progress.CallCount + fixture.Risk.CallCount + fixture.Workload.CallCount);
        Assert.Equal(0, fixture.Writer.CallCount);
        Assert.Equal("# Resumed weekly report", result.Data!.Content);
    }

    [Fact]
    public async Task Review_approves_once_notifies_the_requester_and_rejects_duplicate_delivery()
    {
        var fixture = new Fixture();
        fixture.Reports.Seed(new WeeklyReport
        {
            Id = 41,
            ProjectId = 12,
            RequestedByUserId = 7,
            SourceEventId = fixture.SourceEventId,
            Content = "# Weekly report",
            CreatedAtUtc = new DateTime(2026, 8, 2, 11, 0, 0, DateTimeKind.Utc)
        });

        var first = await fixture.Service.ReviewAsync(41, true, CancellationToken.None);
        var duplicate = await fixture.Service.ReviewAsync(41, true, CancellationToken.None);

        Assert.Equal(WeeklyReportStatus.Approved, first.Data!.Status);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.Status);
        Assert.Equal([(true, 41)], fixture.Notifications.ReviewResults);
        Assert.Equal(["approved"], fixture.Audit.Actions);
        Assert.True(fixture.Reports.Checkpoints.ContainsKey(WeeklyReportCheckpointStages.Approved));
    }

    private sealed class Fixture
    {
        public Guid SourceEventId { get; } = Guid.NewGuid();
        public CreateWeeklyReportRequest Request => new(SourceEventId, new DateOnly(2026, 8, 2));
        public FakeRepository Reports { get; } = new();
        public CountingCollector Collector { get; } = new();
        public CountingAnalyst Progress { get; } = new("Progress", "progress analysis");
        public CountingAnalyst Risk { get; } = new("Risk", "risk analysis");
        public CountingAnalyst Workload { get; } = new("Workload", "workload analysis");
        public CountingWriter Writer { get; } = new();
        public FakeNotifications Notifications { get; } = new();
        public FakeAudit Audit { get; } = new();
        public IWeeklyReportService Service { get; }

        public Fixture()
        {
            Service = new WeeklyReportService(
                Reports,
                new AllowAuthorization(),
                new AllowBackgroundAuthorization(),
                Collector,
                [Progress, Risk, Workload],
                Writer,
                new FakeScheduler(),
                Notifications,
                Audit,
                new FakeUnitOfWork(),
                new FakeClock(),
                new FakeAuditContext());
        }
    }

    private sealed class FakeRepository : IWeeklyReportRepository
    {
        private readonly List<WeeklyReport> _reports = [];
        public Dictionary<string, WeeklyReportCheckpoint> Checkpoints { get; } = [];

        public Task<WeeklyReport?> GetAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult(_reports.SingleOrDefault(x => x.Id == id));

        public Task<WeeklyReport?> GetBySourceAsync(int projectId, Guid source, CancellationToken cancellationToken) =>
            Task.FromResult(_reports.SingleOrDefault(x => x.ProjectId == projectId && x.SourceEventId == source));

        public ValueTask AddAsync(WeeklyReport report, CancellationToken cancellationToken)
        {
            report.Id = _reports.Count + 1;
            _reports.Add(report);
            return ValueTask.CompletedTask;
        }

        public void Seed(WeeklyReport report) => _reports.Add(report);

        public Task UpsertScheduleAsync(WeeklyReportSchedule schedule, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task WriteCheckpointAsync(WeeklyReportCheckpoint checkpoint, CancellationToken cancellationToken)
        {
            Checkpoints[checkpoint.Stage] = checkpoint;
            return Task.CompletedTask;
        }

        public Task<WeeklyReportCheckpoint?> GetCheckpointAsync(int projectId, Guid sourceEventId, string stage, CancellationToken cancellationToken) =>
            Task.FromResult(Checkpoints.GetValueOrDefault(stage));
    }

    private sealed class CountingCollector : IReportDataCollector
    {
        public int CallCount { get; private set; }
        public Task<ReportData> CollectAsync(AgentExecutionScope scope, DateOnly weekEnding, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new ReportData("fresh progress", "fresh risks", "fresh workload"));
        }
    }

    private sealed class FixedProjectDataPort(ReportProjectSnapshot snapshot) : IReportProjectDataPort
    {
        public Task<ReportProjectSnapshot> ReadAsync(AgentExecutionScope scope, DateOnly weekEnding, CancellationToken cancellationToken) =>
            Task.FromResult(snapshot);
    }

    private sealed class CountingAnalyst(string name, string result) : IReportAnalyst
    {
        public string Name { get; } = name;
        public int CallCount { get; private set; }
        public Task<string> AnalyzeAsync(ReportData data, AgentExecutionScope scope, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }

    private sealed class CountingWriter : IReportWriter
    {
        public int CallCount { get; private set; }
        public Task<string> WriteAsync(IReadOnlyDictionary<string, string> analyses, AgentExecutionScope scope, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(string.Join("\n", analyses.Values));
        }
    }

    private sealed class AllowAuthorization : IReportAuthorizationPort
    {
        public Task<AgentExecutionScope?> AuthorizeProjectAsync(int projectId, bool requireManage, CancellationToken cancellationToken) =>
            Task.FromResult<AgentExecutionScope?>(new AgentExecutionScope(7, 5, projectId, Guid.NewGuid(), Guid.NewGuid()));
    }

    private sealed class AllowBackgroundAuthorization : IReportBackgroundAuthorizationPort
    {
        public Task<AgentExecutionScope?> AuthorizeProjectAsync(int userId, int projectId, bool requireManage, Guid correlationId, Guid causationId, CancellationToken cancellationToken) =>
            Task.FromResult<AgentExecutionScope?>(new AgentExecutionScope(userId, 5, projectId, correlationId, causationId));
    }

    private sealed class FakeScheduler : IReportScheduler
    {
        public bool IsValidSchedule(string cron) => true;
        public Task ScheduleAsync(int projectId, int userId, string cron, string timeZoneId, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RemoveAsync(int projectId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeNotifications : IReportNotificationPort
    {
        public List<int> ReviewReadyReportIds { get; } = [];
        public List<(bool Approved, int ReportId)> ReviewResults { get; } = [];
        public Task NotifyReviewAsync(WeeklyReport report, CancellationToken cancellationToken)
        {
            ReviewReadyReportIds.Add(report.Id);
            return Task.CompletedTask;
        }
        public Task NotifyReviewResultAsync(WeeklyReport report, bool approved, CancellationToken cancellationToken)
        {
            ReviewResults.Add((approved, report.Id));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAudit : IReportAuditPort
    {
        public List<string> Actions { get; } = [];
        public Task RecordAsync(WeeklyReport report, string action, CancellationToken cancellationToken)
        {
            Actions.Add(action);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class FakeClock : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = new(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeAuditContext : IAuditContextAccessor
    {
        public int? UserId { get; private set; }
        public string? CorrelationId { get; private set; }

        public IDisposable BeginScope(int? userId, string? correlationId)
        {
            UserId = userId;
            CorrelationId = correlationId;
            return new Scope(() =>
            {
                UserId = null;
                CorrelationId = null;
            });
        }

        private sealed class Scope(Action dispose) : IDisposable
        {
            private Action? _dispose = dispose;
            public void Dispose() => Interlocked.Exchange(ref _dispose, null)?.Invoke();
        }
    }
}
