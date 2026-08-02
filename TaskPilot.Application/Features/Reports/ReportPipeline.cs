using System.Text.Json;
using TaskPilot.Application.Features.Ai;
using TaskPilot.Domain.AI;

namespace TaskPilot.Application.Features.Reports;

public sealed class ReportDataCollector(IReportProjectDataPort dataPort) : IReportDataCollector
{
    public async Task<ReportData> CollectAsync(TaskPilot.Domain.AI.AgentExecutionScope scope, DateOnly weekEnding, CancellationToken ct)
    {
        var snapshot = await dataPort.ReadAsync(scope, weekEnding, ct);
        return new ReportData(
            $"{snapshot.CompletedTasks}/{snapshot.TotalTasks} tasks completed. Recently completed: {string.Join(", ", snapshot.RecentlyCompletedTitles)}. {snapshot.NewCommentCount} new comments.",
            $"{snapshot.OverdueTasks} overdue tasks.",
            $"{snapshot.UnassignedOpenTasks} open tasks are unassigned. {snapshot.RecentlyUpdatedAssignedTaskCount} assigned tasks changed this week.");
    }
}

public abstract class AiReportAnalyst(IAiChatGenerator generator) : IReportAnalyst
{
    public abstract string Name { get; }
    protected abstract string Instructions { get; }

    public Task<string> AnalyzeAsync(ReportData data, AgentExecutionScope scope, CancellationToken ct) =>
        generator.CompleteAsync(
            [
                new AiChatMessage("system", Instructions + " Use only the supplied authorized weekly snapshot; do not invent facts."),
                new AiChatMessage("user", JsonSerializer.Serialize(data))
            ],
            scope,
            ct);
}

public sealed class ProgressReportAnalyst(IAiChatGenerator generator) : AiReportAnalyst(generator)
{
    public override string Name => "Progress";
    protected override string Instructions => "Analyze delivery progress and summarize concrete achievements and trend signals.";
}

public sealed class RiskReportAnalyst(IAiChatGenerator generator) : AiReportAnalyst(generator)
{
    public override string Name => "Risk";
    protected override string Instructions => "Analyze schedule and delivery risks, separating evidence from cautious inference.";
}

public sealed class WorkloadReportAnalyst(IAiChatGenerator generator) : AiReportAnalyst(generator)
{
    public override string Name => "Workload";
    protected override string Instructions => "Analyze workload distribution and capacity concerns without identifying personal data.";
}

public sealed class MarkdownReportWriter(IAiChatGenerator generator) : IReportWriter
{
    public Task<string> WriteAsync(
        IReadOnlyDictionary<string, string> analyses,
        AgentExecutionScope scope,
        CancellationToken ct) =>
        generator.CompleteAsync(
            [
                new AiChatMessage(
                    "system",
                    "Merge the supplied Progress, Risk, and Workload analyses into a concise Markdown weekly report. " +
                    "Use exactly those three level-two sections, retain uncertainty, and add no unsupported facts."),
                new AiChatMessage("user", JsonSerializer.Serialize(analyses))
            ],
            scope,
            ct);
}
