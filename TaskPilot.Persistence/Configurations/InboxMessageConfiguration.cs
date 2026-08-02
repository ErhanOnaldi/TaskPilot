using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskPilot.Persistence.Messaging;

namespace TaskPilot.Persistence.Configurations;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.ConsumerName).HasMaxLength(200).IsRequired();
        builder.HasIndex(message => new { message.ConsumerName, message.EventId }).IsUnique();
        builder.HasIndex(message => message.ProcessedAtUtc);
        builder.HasIndex(message => message.LockedUntilUtc);
    }
}
