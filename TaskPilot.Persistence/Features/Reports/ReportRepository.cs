using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Features.Reports;
using TaskPilot.Application.Interfaces.Infrastructure.Auditing;
using TaskPilot.Application.Interfaces.Infrastructure;
using TaskPilot.Domain.AI;
using TaskPilot.Domain.AI.Reports;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.Features.Reports;

public sealed class EfWeeklyReportRepository(AppDbContext db) : IWeeklyReportRepository
{
    public Task<WeeklyReport?> GetAsync(int id, CancellationToken ct) => db.Set<WeeklyReport>().FindAsync([id], ct).AsTask();
    public Task<WeeklyReport?> GetBySourceAsync(int projectId, Guid source, CancellationToken ct) => db.Set<WeeklyReport>().SingleOrDefaultAsync(x => x.ProjectId == projectId && x.SourceEventId == source, ct);
    public async ValueTask AddAsync(WeeklyReport report, CancellationToken ct) => await db.Set<WeeklyReport>().AddAsync(report, ct);

    public async Task UpsertScheduleAsync(WeeklyReportSchedule schedule, CancellationToken ct)
    {
        var existing = await db.Set<WeeklyReportSchedule>().SingleOrDefaultAsync(x => x.ProjectId == schedule.ProjectId, ct);
        if (existing is null) await db.Set<WeeklyReportSchedule>().AddAsync(schedule, ct);
        else
        {
            existing.RequestedByUserId = schedule.RequestedByUserId;
            existing.Cron = schedule.Cron;
            existing.TimeZoneId = schedule.TimeZoneId;
            existing.Enabled = schedule.Enabled;
        }
    }
    public async Task WriteCheckpointAsync(WeeklyReportCheckpoint checkpoint,CancellationToken ct){var existing=await GetCheckpointAsync(checkpoint.ProjectId,checkpoint.SourceEventId,checkpoint.Stage,ct);if(existing is null)await db.Set<WeeklyReportCheckpoint>().AddAsync(checkpoint,ct);else{existing.PayloadJson=checkpoint.PayloadJson;existing.UpdatedAtUtc=checkpoint.UpdatedAtUtc;}}
    public Task<WeeklyReportCheckpoint?> GetCheckpointAsync(int projectId,Guid sourceEventId,string stage,CancellationToken ct)=>db.Set<WeeklyReportCheckpoint>().SingleOrDefaultAsync(x=>x.ProjectId==projectId&&x.SourceEventId==sourceEventId&&x.Stage==stage,ct);
}

public sealed class ReportBackgroundAuthorizationPort(AppDbContext db) : IReportBackgroundAuthorizationPort
{
    public async Task<AgentExecutionScope?> AuthorizeProjectAsync(
        int userId,
        int projectId,
        bool requireManage,
        Guid correlationId,
        Guid causationId,
        CancellationToken cancellationToken)
    {
        var access = await db.Set<Project>()
            .Where(project => project.Id == projectId &&
                              project.Status != ProjectStatus.Archived &&
                              project.WorkSpace != null &&
                              !project.WorkSpace.IsArchived)
            .Select(project => new
            {
                project.WorkspaceId,
                WorkspaceRole = db.Set<WorkspaceMember>()
                    .Where(member => member.WorkspaceId == project.WorkspaceId && member.UserId == userId)
                    .Select(member => (Role?)member.Role)
                    .SingleOrDefault(),
                ProjectRole = db.Set<ProjectMember>()
                    .Where(member => member.ProjectId == project.Id && member.UserId == userId)
                    .Select(member => (ProjectRole?)member.Role)
                    .SingleOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (access?.WorkspaceRole is null)
            return null;
        var allowed = access.WorkspaceRole == Role.Owner ||
                      (requireManage
                          ? access.ProjectRole == TaskPilot.Domain.Entities.ProjectRole.ProjectManager
                          : access.ProjectRole is not null);
        return allowed
            ? new AgentExecutionScope(userId, access.WorkspaceId, projectId, correlationId, causationId)
            : null;
    }
}

public sealed class ReportProjectDataRepository(AppDbContext db) : IReportProjectDataPort
{
    public async Task<ReportProjectSnapshot> ReadAsync(AgentExecutionScope scope, DateOnly weekEnding, CancellationToken ct)
    {
        var projectId = scope.ProjectId ?? throw new InvalidOperationException("Project report scope is required.");
        var weekEnd = weekEnding.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var weekStart = weekEnd.AddDays(-7);
        var tasks = await db.Set<TaskItem>()
            .Where(x => x.ProjectId == projectId && x.Project!.WorkspaceId == scope.WorkspaceId && x.Project.Status != ProjectStatus.Archived)
            .Select(x => new { x.Title, x.Status, x.DueDate, x.AssignedUserId, x.CompletedAt, x.UpdatedAt })
            .ToListAsync(ct);
        return new ReportProjectSnapshot(
            tasks.Count,
            tasks.Count(x => x.Status == TaskItemStatus.Done),
            tasks.Count(x => x.Status != TaskItemStatus.Done && x.DueDate < weekEnd),
            tasks.Count(x => x.Status != TaskItemStatus.Done && x.AssignedUserId == null),
            tasks.Where(x => x.CompletedAt >= weekStart && x.CompletedAt <= weekEnd).Select(x => x.Title).Take(20).ToList(),
            await db.Set<Comment>().CountAsync(comment =>
                comment.TaskItem!.ProjectId == projectId &&
                comment.TaskItem.Project!.WorkspaceId == scope.WorkspaceId &&
                comment.CreatedAt >= weekStart && comment.CreatedAt <= weekEnd, ct),
            tasks.Count(x => x.AssignedUserId != null && x.UpdatedAt >= weekStart && x.UpdatedAt <= weekEnd));
    }
}

public sealed class ReportNotificationPort(AppDbContext db, IDateTimeProvider clock) : IReportNotificationPort
{
    public async Task NotifyReviewAsync(WeeklyReport report, CancellationToken ct)
    {
        var sourceEventId = ReportNotificationSourceId.Create(report.SourceEventId, "review-ready");
        if (await db.Set<Notification>().AnyAsync(x => x.UserId == report.RequestedByUserId && x.SourceEventId == sourceEventId, ct)) return;
        var now = clock.UtcNow;
        await db.Set<Notification>().AddAsync(new Notification
        {
            UserId = report.RequestedByUserId,
            Type = "WeeklyReportReview",
            Title = "Weekly report ready for review",
            Message = "A weekly project report is ready for approval.",
            SourceEventId = sourceEventId,
            RelatedEntityId = report.ProjectId,
            CreatedAt = now,
            UpdatedAt = now
        }, ct);
    }

    public async Task NotifyReviewResultAsync(WeeklyReport report, bool approved, CancellationToken ct)
    {
        var sourceEventId = ReportNotificationSourceId.Create(report.SourceEventId, "review-result");
        if (await db.Set<Notification>().AnyAsync(x => x.UserId == report.RequestedByUserId && x.SourceEventId == sourceEventId, ct)) return;
        var now = clock.UtcNow;
        await db.Set<Notification>().AddAsync(new Notification
        {
            UserId = report.RequestedByUserId,
            Type = approved ? "WeeklyReportApproved" : "WeeklyReportRejected",
            Title = approved ? "Weekly report approved" : "Weekly report rejected",
            Message = approved
                ? "Your weekly project report was approved."
                : "Your weekly project report was rejected.",
            SourceEventId = sourceEventId,
            RelatedEntityId = report.ProjectId,
            CreatedAt = now,
            UpdatedAt = now
        }, ct);
    }
}

public sealed class ReportAuditBridge(AppDbContext db, IDateTimeProvider clock, IAuditContextAccessor auditContext) : IReportAuditPort
{
    public Task RecordAsync(WeeklyReport report, string action, CancellationToken ct) =>
        db.AuditLogs.AddAsync(new AuditLog
        {
            UserId = auditContext.UserId ?? report.RequestedByUserId,
            EntityName = "WeeklyReportWorkflow",
            EntityId = report.ProjectId,
            Action = $"Workflow{char.ToUpperInvariant(action[0])}{action[1..]}",
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                report.SourceEventId,
                report.Status,
                report.RequestedByUserId
            }),
            CreatedAt = clock.UtcNow,
            CorrelationId = auditContext.CorrelationId
        }, ct).AsTask();
}

internal static class ReportNotificationSourceId
{
    public static Guid Create(Guid sourceEventId, string purpose)
    {
        var purposeBytes = System.Text.Encoding.UTF8.GetBytes(purpose);
        Span<byte> input = stackalloc byte[16 + purposeBytes.Length];
        sourceEventId.TryWriteBytes(input);
        purposeBytes.CopyTo(input[16..]);
        Span<byte> hash = stackalloc byte[32];
        System.Security.Cryptography.SHA256.HashData(input[..(16 + purposeBytes.Length)], hash);
        return new Guid(hash[..16]);
    }
}

public sealed class WeeklyReportConfiguration : IEntityTypeConfiguration<WeeklyReport>
{
    public void Configure(EntityTypeBuilder<WeeklyReport> builder)
    {
        builder.ToTable("WeeklyReports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.Content).IsRequired();
        builder.HasIndex(x => new { x.ProjectId, x.SourceEventId }).IsUnique();
        builder.HasIndex(x => new { x.ProjectId, x.CreatedAtUtc });
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class WeeklyReportCheckpointConfiguration : IEntityTypeConfiguration<WeeklyReportCheckpoint>
{
    public void Configure(EntityTypeBuilder<WeeklyReportCheckpoint> builder)
    {
        builder.ToTable("WeeklyReportCheckpoints"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Stage).HasMaxLength(100).IsRequired(); builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => new { x.ProjectId, x.SourceEventId, x.Stage }).IsUnique();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class WeeklyReportScheduleConfiguration : IEntityTypeConfiguration<WeeklyReportSchedule>
{
    public void Configure(EntityTypeBuilder<WeeklyReportSchedule> builder)
    {
        builder.ToTable("WeeklyReportSchedules"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Cron).HasMaxLength(200).IsRequired(); builder.Property(x => x.TimeZoneId).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.ProjectId).IsUnique();
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public static class ReportPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddReportPersistence(this IServiceCollection services) => services
        .AddScoped<IWeeklyReportRepository, EfWeeklyReportRepository>()
        .AddScoped<IReportBackgroundAuthorizationPort, ReportBackgroundAuthorizationPort>()
        .AddScoped<IReportProjectDataPort, ReportProjectDataRepository>()
        .AddScoped<IReportNotificationPort, ReportNotificationPort>()
        .AddScoped<IReportAuditPort, ReportAuditBridge>();
}
