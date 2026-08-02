using TaskPilot.Domain.AI;
using TaskPilot.Domain.AI.Reports;

namespace TaskPilot.Application.Features.Reports;

public sealed record CreateWeeklyReportRequest(Guid SourceEventId, DateOnly WeekEnding);
public sealed record UpdateReportScheduleRequest(string Cron, string TimeZoneId, bool Enabled);
public sealed record WeeklyReportResponse(
    int Id,
    int ProjectId,
    WeeklyReportStatus Status,
    string Content,
    DateTime CreatedAtUtc,
    DateTime? ReviewedAtUtc);

public sealed record ReportData(string Progress, string Risks, string Workload);
public sealed record ReportProjectSnapshot(
    int TotalTasks,
    int CompletedTasks,
    int OverdueTasks,
    int UnassignedOpenTasks,
    IReadOnlyList<string> RecentlyCompletedTitles,
    int NewCommentCount,
    int RecentlyUpdatedAssignedTaskCount);

public interface IWeeklyReportRepository
{
    Task<WeeklyReport?> GetAsync(int id, CancellationToken cancellationToken);
    Task<WeeklyReport?> GetBySourceAsync(int projectId, Guid source, CancellationToken cancellationToken);
    ValueTask AddAsync(WeeklyReport report, CancellationToken cancellationToken);
    Task UpsertScheduleAsync(WeeklyReportSchedule schedule, CancellationToken cancellationToken);
    Task WriteCheckpointAsync(WeeklyReportCheckpoint checkpoint, CancellationToken cancellationToken);
    Task<WeeklyReportCheckpoint?> GetCheckpointAsync(int projectId, Guid sourceEventId, string stage, CancellationToken cancellationToken);
}

public interface IReportDataCollector
{
    Task<ReportData> CollectAsync(AgentExecutionScope scope, DateOnly weekEnding, CancellationToken cancellationToken);
}

public interface IReportProjectDataPort
{
    Task<ReportProjectSnapshot> ReadAsync(AgentExecutionScope scope, DateOnly weekEnding, CancellationToken cancellationToken);
}

public interface IReportAnalyst
{
    string Name { get; }
    Task<string> AnalyzeAsync(ReportData data, AgentExecutionScope scope, CancellationToken cancellationToken);
}

public interface IReportWriter
{
    Task<string> WriteAsync(
        IReadOnlyDictionary<string, string> analyses,
        AgentExecutionScope scope,
        CancellationToken cancellationToken);
}

public interface IReportAuthorizationPort
{
    Task<AgentExecutionScope?> AuthorizeProjectAsync(int projectId, bool requireManage, CancellationToken cancellationToken);
}

public interface IReportBackgroundAuthorizationPort
{
    Task<AgentExecutionScope?> AuthorizeProjectAsync(
        int userId,
        int projectId,
        bool requireManage,
        Guid correlationId,
        Guid causationId,
        CancellationToken cancellationToken);
}

public interface IReportScheduler
{
    bool IsValidSchedule(string cron);
    Task ScheduleAsync(int projectId, int userId, string cron, string timeZoneId, CancellationToken cancellationToken);
    Task RemoveAsync(int projectId, CancellationToken cancellationToken);
}

public interface IReportNotificationPort
{
    Task NotifyReviewAsync(WeeklyReport report, CancellationToken cancellationToken);
    Task NotifyReviewResultAsync(WeeklyReport report, bool approved, CancellationToken cancellationToken);
}

public interface IReportAuditPort
{
    Task RecordAsync(WeeklyReport report, string action, CancellationToken cancellationToken);
}

public static class WeeklyReportCheckpointStages
{
    public const string Authorized = "authorized";
    public const string Collected = "collected";
    public const string Written = "written";
    public const string PendingReview = "pending-review";
    public const string Approved = "approved";
    public const string Rejected = "rejected";

    public static string Analysis(string analystName) => $"analysis:{analystName}";
}

public interface IWeeklyReportService
{
    Task<ServiceResult<WeeklyReportResponse>> CreateAsync(
        int projectId,
        CreateWeeklyReportRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<WeeklyReportResponse>> CreateScheduledAsync(
        int projectId,
        int requestedByUserId,
        CreateWeeklyReportRequest request,
        Guid correlationId,
        Guid causationId,
        CancellationToken cancellationToken);

    Task<ServiceResult<WeeklyReportResponse>> GetAsync(int reportId, CancellationToken cancellationToken);
    Task<ServiceResult<WeeklyReportResponse>> ReviewAsync(int reportId, bool approve, CancellationToken cancellationToken);
    Task<ServiceResult> UpdateScheduleAsync(
        int projectId,
        UpdateReportScheduleRequest request,
        CancellationToken cancellationToken);
}
