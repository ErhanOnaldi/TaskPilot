using System.Collections.Concurrent;
using TaskPilot.Application.Features.Ai;
using TaskPilot.Application.Features.Reports;
using TaskPilot.Domain.AI;

namespace TaskPilot.Application.Tests;

public sealed class WeeklyReportAiPipelineTests
{
    [Fact]
    public async Task Three_specialized_analysts_run_as_independent_ai_calls_and_writer_fans_in()
    {
        var generator = new RecordingGenerator();
        IReportAnalyst[] analysts =
        [
            new ProgressReportAnalyst(generator),
            new RiskReportAnalyst(generator),
            new WorkloadReportAnalyst(generator)
        ];
        var scope = new AgentExecutionScope(7, 11, 13, Guid.NewGuid(), Guid.NewGuid());
        var data = new ReportData("progress", "risks", "workload");

        var results = await Task.WhenAll(analysts.Select(async analyst =>
            new KeyValuePair<string, string>(
                analyst.Name,
                await analyst.AnalyzeAsync(data, scope, CancellationToken.None))));
        var report = await new MarkdownReportWriter(generator).WriteAsync(
            results.ToDictionary(result => result.Key, result => result.Value),
            scope,
            CancellationToken.None);

        Assert.Equal(4, generator.Calls.Count);
        Assert.Contains(generator.Calls, call => call.Contains("delivery progress", StringComparison.Ordinal));
        Assert.Contains(generator.Calls, call => call.Contains("schedule and delivery risks", StringComparison.Ordinal));
        Assert.Contains(generator.Calls, call => call.Contains("workload distribution", StringComparison.Ordinal));
        Assert.Contains(generator.Calls, call => call.Contains("Merge the supplied", StringComparison.Ordinal));
        Assert.Equal("generated", report);
    }

    private sealed class RecordingGenerator : IAiChatGenerator
    {
        public ConcurrentBag<string> Calls { get; } = [];

        public Task<string> CompleteAsync(
            IReadOnlyList<AiChatMessage> messages,
            AgentExecutionScope scope,
            CancellationToken cancellationToken)
        {
            Calls.Add(string.Join("\n", messages.Select(message => message.Content)));
            return Task.FromResult("generated");
        }

        public async IAsyncEnumerable<string> StreamAsync(
            IReadOnlyList<AiChatMessage> messages,
            AgentExecutionScope scope,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return await CompleteAsync(messages, scope, cancellationToken);
        }
    }
}
