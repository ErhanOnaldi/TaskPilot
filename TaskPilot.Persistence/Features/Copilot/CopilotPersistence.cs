using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using TaskPilot.Application.Features.Copilot;
using TaskPilot.Domain.AI.Copilot;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.Features.Copilot;

public sealed class CopilotChatSessionConfiguration : IEntityTypeConfiguration<CopilotChatSession>
{
    public void Configure(EntityTypeBuilder<CopilotChatSession> builder)
    {
        builder.ToTable("CopilotChatSessions"); builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.WorkspaceId, x.ProjectId, x.UpdatedAtUtc });
        builder.HasMany(x => x.Messages).WithOne().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<WorkSpace>().WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
public sealed class CopilotChatMessageConfiguration : IEntityTypeConfiguration<CopilotChatMessage>
{
    public void Configure(EntityTypeBuilder<CopilotChatMessage> builder) { builder.ToTable("CopilotChatMessages"); builder.HasKey(x => x.Id); builder.Property(x => x.Content).IsRequired(); builder.Property(x => x.CitationsJson).HasColumnType("jsonb").IsRequired(); builder.HasIndex(x => new { x.SessionId, x.CreatedAtUtc }); }
}
public sealed class EfCopilotRepository(AppDbContext db) : ICopilotRepository
{
    public Task<CopilotChatSession?> GetSessionAsync(int id, CancellationToken ct) => db.Set<CopilotChatSession>().Include(x => x.Messages).SingleOrDefaultAsync(x => x.Id == id, ct);
    public ValueTask AddSessionAsync(CopilotChatSession session, CancellationToken ct) => new(db.Set<CopilotChatSession>().AddAsync(session, ct).AsTask());
    public ValueTask AddMessageAsync(CopilotChatMessage message, CancellationToken ct) => new(db.Set<CopilotChatMessage>().AddAsync(message, ct).AsTask());
}
public static class CopilotPersistenceServiceCollectionExtensions { public static IServiceCollection AddCopilotPersistence(this IServiceCollection services) { services.AddScoped<ICopilotRepository, EfCopilotRepository>(); return services; } }
