using TaskPilot.Application.Events;
using TaskPilot.Application.Messaging;

namespace TaskPilot.Application.Tests;

public sealed class IntegrationEventEnvelopeTests
{
    [Fact]
    public void Create_captures_standard_metadata_and_serialized_payload()
    {
        var eventId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();
        var occurredAt = new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc);
        var integrationEvent = new TaskAssignedEvent(eventId, 10, 20, 2, 1, occurredAt)
        {
            CorrelationId = correlationId,
            CausationId = causationId
        };

        var envelope = IntegrationEventEnvelope.Create(integrationEvent);

        Assert.Equal(eventId, envelope.EventId);
        Assert.Equal(correlationId, envelope.CorrelationId);
        Assert.Equal(causationId, envelope.CausationId);
        Assert.Equal("task.assigned", envelope.EventType);
        Assert.Equal(1, envelope.SchemaVersion);
        Assert.Equal(occurredAt, envelope.OccurredAt);
        Assert.Contains("AssignedUserId", envelope.Payload, StringComparison.Ordinal);
    }
}
