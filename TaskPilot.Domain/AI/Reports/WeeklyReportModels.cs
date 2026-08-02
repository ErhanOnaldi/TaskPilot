using TaskPilot.Domain.Entities;

namespace TaskPilot.Domain.AI.Reports;

public enum WeeklyReportStatus { PendingReview, Approved, Rejected }

public sealed class WeeklyReport : AuditEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int RequestedByUserId { get; set; }
    public Guid SourceEventId { get; set; }
    public WeeklyReportStatus Status { get; set; } = WeeklyReportStatus.PendingReview;
    public string Content { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public int Version { get; set; } = 1;
}

public sealed class WeeklyReportCheckpoint
{
    public int Id { get; set; }
    public Guid SourceEventId { get; set; }
    public int ProjectId { get; set; }
    public string Stage { get; set; } = null!;
    public string PayloadJson { get; set; } = "{}";
    public DateTime UpdatedAtUtc { get; set; }
}

public sealed class WeeklyReportSchedule
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int RequestedByUserId { get; set; }
    public string Cron { get; set; } = null!;
    public string TimeZoneId { get; set; } = null!;
    public bool Enabled { get; set; }
}
