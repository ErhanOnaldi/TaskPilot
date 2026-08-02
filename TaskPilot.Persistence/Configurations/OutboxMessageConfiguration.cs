using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskPilot.Persistence.Messaging;

namespace TaskPilot.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.EventType).HasMaxLength(200).IsRequired();
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(4000);
        builder.HasIndex(message => message.EventId).IsUnique();
        builder.HasIndex(message => new { message.DispatchedAtUtc, message.CreatedAtUtc });
        builder.HasIndex(message => new { message.DeadLetteredAtUtc, message.LockedUntilUtc, message.CreatedAtUtc });
        builder.HasIndex(message => message.LockId);
    }
}
