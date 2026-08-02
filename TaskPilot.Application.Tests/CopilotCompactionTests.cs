using TaskPilot.Application.Features.Copilot;
using TaskPilot.Domain.AI.Copilot;

namespace TaskPilot.Application.Tests;

public sealed class CopilotCompactionTests
{
    [Fact]
    public void Compact_preserves_recent_messages_and_adds_deterministic_summary()
    {
        var messages = Enumerable.Range(1, 15).Select(i => new CopilotChatMessage { Role = i % 2 == 0 ? CopilotMessageRole.Assistant : CopilotMessageRole.User, Content = $"message-{i}", CreatedAtUtc = new DateTime(2026, 8, 2).AddMinutes(i) });
        var compacted = CopilotService.Compact(messages);
        Assert.Equal(12, compacted.Count);
        Assert.StartsWith("Conversation summary (deterministic):", compacted[0].Content);
        Assert.Equal("message-15", compacted[^1].Content);
    }
}
