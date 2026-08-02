using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Application.Interfaces.Infrastructure.Auditing;
using TaskPilot.Application.Interfaces.Persistence;
using TaskPilot.Domain.AI;
using TaskPilot.Domain.AI.Reports;

namespace TaskPilot.Application.Features.Reports;

public sealed class WeeklyReportService(
    IWeeklyReportRepository reports,
    IReportAuthorizationPort authorization,
    IReportBackgroundAuthorizationPort backgroundAuthorization,
    IReportDataCollector collector,
    IEnumerable<IReportAnalyst> analysts,
    IReportWriter writer,
    IReportScheduler scheduler,
    IReportNotificationPort notifications,
    IReportAuditPort audit,
    IUnitOfWork unitOfWork,
    IDateTimeProvider clock,
    IAuditContextAccessor auditContext) : IWeeklyReportService
{
    public async Task<ServiceResult<WeeklyReportResponse>> CreateAsync(
        int projectId,
        CreateWeeklyReportRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await authorization.AuthorizeProjectAsync(projectId, false, cancellationToken);
        if (scope is null)
            return ServiceResult<WeeklyReportResponse>.Fail("Project membership is required.", HttpStatusCode.Forbidden);
        using var auditScope = auditContext.BeginScope(scope.UserId, scope.CorrelationId.ToString());
        return await CreateForScopeAsync(scope, request, cancellationToken);
    }

    public async Task<ServiceResult<WeeklyReportResponse>> CreateScheduledAsync(
        int projectId,
        int requestedByUserId,
        CreateWeeklyReportRequest request,
        Guid correlationId,
        Guid causationId,
        CancellationToken cancellationToken)
    {
        var scope = await backgroundAuthorization.AuthorizeProjectAsync(
            requestedByUserId,
            projectId,
            true,
            correlationId,
            causationId,
            cancellationToken);
        if (scope is null)
            return ServiceResult<WeeklyReportResponse>.Fail("Project management permission is required.", HttpStatusCode.Forbidden);
        using var auditScope = auditContext.BeginScope(scope.UserId, scope.CorrelationId.ToString());
        return await CreateForScopeAsync(scope, request, cancellationToken);
    }

    public async Task<ServiceResult<WeeklyReportResponse>> GetAsync(int id, CancellationToken cancellationToken)
    {
        var report = await reports.GetAsync(id, cancellationToken);
        if (report is null)
            return ServiceResult<WeeklyReportResponse>.Fail("Report not found.", HttpStatusCode.NotFound);
        var scope = await authorization.AuthorizeProjectAsync(report.ProjectId, false, cancellationToken);
        return scope is null
            ? ServiceResult<WeeklyReportResponse>.Fail("Project membership is required.", HttpStatusCode.Forbidden)
            : ServiceResult<WeeklyReportResponse>.Success(Map(report));
    }

    public async Task<ServiceResult<WeeklyReportResponse>> ReviewAsync(
        int id,
        bool approve,
        CancellationToken cancellationToken)
    {
        var report = await reports.GetAsync(id, cancellationToken);
        if (report is null)
            return ServiceResult<WeeklyReportResponse>.Fail("Report not found.", HttpStatusCode.NotFound);
        var scope = await authorization.AuthorizeProjectAsync(report.ProjectId, true, cancellationToken);
        if (scope is null)
            return ServiceResult<WeeklyReportResponse>.Fail("Project management permission is required.", HttpStatusCode.Forbidden);
        if (report.Status != WeeklyReportStatus.PendingReview)
            return ServiceResult<WeeklyReportResponse>.Fail("Report has already been reviewed.", HttpStatusCode.Conflict);

        using var auditScope = auditContext.BeginScope(scope.UserId, scope.CorrelationId.ToString());
        report.Status = approve ? WeeklyReportStatus.Approved : WeeklyReportStatus.Rejected;
        report.ReviewedAtUtc = clock.UtcNow;
        report.Version++;
        await reports.WriteCheckpointAsync(new WeeklyReportCheckpoint
        {
            ProjectId = report.ProjectId,
            SourceEventId = report.SourceEventId,
            Stage = approve ? WeeklyReportCheckpointStages.Approved : WeeklyReportCheckpointStages.Rejected,
            UpdatedAtUtc = clock.UtcNow
        }, cancellationToken);
        await notifications.NotifyReviewResultAsync(report, approve, cancellationToken);
        await audit.RecordAsync(report, approve ? "approved" : "rejected", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<WeeklyReportResponse>.Success(Map(report));
    }

    public async Task<ServiceResult> UpdateScheduleAsync(
        int projectId,
        UpdateReportScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var scope = await authorization.AuthorizeProjectAsync(projectId, true, cancellationToken);
        if (scope is null)
            return ServiceResult.Fail("Project management permission is required.", HttpStatusCode.Forbidden);
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(request.TimeZoneId, out _))
            return ServiceResult.Fail("Time zone is invalid.");
        if (!scheduler.IsValidSchedule(request.Cron))
            return ServiceResult.Fail("Cron expression is invalid.");

        await reports.UpsertScheduleAsync(new WeeklyReportSchedule
        {
            ProjectId = projectId,
            RequestedByUserId = scope.UserId,
            Cron = request.Cron,
            TimeZoneId = request.TimeZoneId,
            Enabled = request.Enabled
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (request.Enabled)
            await scheduler.ScheduleAsync(projectId, scope.UserId, request.Cron, request.TimeZoneId, cancellationToken);
        else
            await scheduler.RemoveAsync(projectId, cancellationToken);
        return ServiceResult.Success();
    }

    private async Task<ServiceResult<WeeklyReportResponse>> CreateForScopeAsync(
        AgentExecutionScope scope,
        CreateWeeklyReportRequest request,
        CancellationToken cancellationToken)
    {
        var projectId = scope.ProjectId ?? throw new InvalidOperationException("Project scope is required.");
        var existing = await reports.GetBySourceAsync(projectId, request.SourceEventId, cancellationToken);
        if (existing is not null)
            return ServiceResult<WeeklyReportResponse>.Success(Map(existing));

        await WriteCheckpointIfMissingAsync(projectId, request.SourceEventId, WeeklyReportCheckpointStages.Authorized, "{}", cancellationToken);

        var data = await ReadCheckpointAsync<ReportData>(projectId, request.SourceEventId, WeeklyReportCheckpointStages.Collected, cancellationToken)
                   ?? await CollectAndCheckpointAsync();

        var analystList = analysts.ToArray();
        var duplicateName = analystList.GroupBy(analyst => analyst.Name, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicateName is not null)
            throw new InvalidOperationException($"Weekly report analyst '{duplicateName.Key}' is registered more than once.");

        var analyses = new Dictionary<string, string>(StringComparer.Ordinal);
        var analystsToRun = new List<IReportAnalyst>();
        foreach (var analyst in analystList)
        {
            var completedAnalysis = await ReadCheckpointAsync<string>(projectId, request.SourceEventId, WeeklyReportCheckpointStages.Analysis(analyst.Name), cancellationToken);
            if (completedAnalysis is null)
                analystsToRun.Add(analyst);
            else
                analyses.Add(analyst.Name, completedAnalysis);
        }

        // The analysts execute as an independent fan-out superstep. Checkpoints are
        // persisted after the join so the shared persistence context is never used concurrently.
        var newlyCompletedAnalyses = await Task.WhenAll(analystsToRun.Select(async analyst =>
            new KeyValuePair<string, string>(analyst.Name, await analyst.AnalyzeAsync(data, scope, cancellationToken))));
        foreach (var analysis in newlyCompletedAnalyses)
        {
            analyses.Add(analysis.Key, analysis.Value);
            await WriteCheckpointIfMissingAsync(
                projectId,
                request.SourceEventId,
                WeeklyReportCheckpointStages.Analysis(analysis.Key),
                JsonSerializer.Serialize(analysis.Value),
                cancellationToken);
        }

        var content = await ReadCheckpointAsync<string>(projectId, request.SourceEventId, WeeklyReportCheckpointStages.Written, cancellationToken)
                      ?? await WriteAndCheckpointAsync();
        var report = new WeeklyReport
        {
            ProjectId = projectId,
            RequestedByUserId = scope.UserId,
            SourceEventId = request.SourceEventId,
            Content = content,
            CreatedAtUtc = clock.UtcNow
        };
        await reports.AddAsync(report, cancellationToken);
        await reports.WriteCheckpointAsync(new WeeklyReportCheckpoint
        {
            ProjectId = projectId,
            SourceEventId = request.SourceEventId,
            Stage = WeeklyReportCheckpointStages.PendingReview,
            UpdatedAtUtc = clock.UtcNow
        }, cancellationToken);
        await notifications.NotifyReviewAsync(report, cancellationToken);
        await audit.RecordAsync(report, "created", cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<WeeklyReportResponse>.Success(Map(report), HttpStatusCode.Created);

        async Task<ReportData> CollectAndCheckpointAsync()
        {
            var collected = await collector.CollectAsync(scope, request.WeekEnding, cancellationToken);
            await WriteCheckpointIfMissingAsync(
                projectId,
                request.SourceEventId,
                WeeklyReportCheckpointStages.Collected,
                JsonSerializer.Serialize(collected),
                cancellationToken);
            return collected;
        }

        async Task<string> WriteAndCheckpointAsync()
        {
            var written = await writer.WriteAsync(analyses, scope, cancellationToken);
            await WriteCheckpointIfMissingAsync(
                projectId,
                request.SourceEventId,
                WeeklyReportCheckpointStages.Written,
                JsonSerializer.Serialize(written),
                cancellationToken);
            return written;
        }
    }

    private async Task<T?> ReadCheckpointAsync<T>(
        int projectId,
        Guid sourceEventId,
        string stage,
        CancellationToken cancellationToken)
    {
        var checkpoint = await reports.GetCheckpointAsync(projectId, sourceEventId, stage, cancellationToken);
        return checkpoint is null
            ? default
            : JsonSerializer.Deserialize<T>(checkpoint.PayloadJson)
              ?? throw new InvalidOperationException($"Weekly report checkpoint '{stage}' has no payload.");
    }

    private async Task WriteCheckpointIfMissingAsync(
        int projectId,
        Guid sourceEventId,
        string stage,
        string payloadJson,
        CancellationToken cancellationToken)
    {
        if (await reports.GetCheckpointAsync(projectId, sourceEventId, stage, cancellationToken) is not null)
            return;

        await reports.WriteCheckpointAsync(new WeeklyReportCheckpoint
        {
            ProjectId = projectId,
            SourceEventId = sourceEventId,
            Stage = stage,
            PayloadJson = payloadJson,
            UpdatedAtUtc = clock.UtcNow
        }, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static WeeklyReportResponse Map(WeeklyReport report) => new(
        report.Id,
        report.ProjectId,
        report.Status,
        report.Content,
        report.CreatedAtUtc,
        report.ReviewedAtUtc);
}

public static class ReportApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddReportApplication(this IServiceCollection services) => services
        .AddScoped<IWeeklyReportService, WeeklyReportService>()
        .AddScoped<IReportDataCollector, ReportDataCollector>()
        .AddScoped<IReportAnalyst, ProgressReportAnalyst>()
        .AddScoped<IReportAnalyst, RiskReportAnalyst>()
        .AddScoped<IReportAnalyst, WorkloadReportAnalyst>()
        .AddScoped<IReportWriter, MarkdownReportWriter>();
}
