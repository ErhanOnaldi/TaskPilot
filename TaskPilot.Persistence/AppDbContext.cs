using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<WorkSpace> WorkSpaces => Set<WorkSpace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<TaskLabel> TaskLabels => Set<TaskLabel>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AiSuggestion> AiSuggestions => Set<AiSuggestion>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(320);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.PasswordHash).IsRequired();
        });

        modelBuilder.Entity<WorkSpace>(entity =>
        {
            entity.ToTable("WorkSpaces");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.ToTable("WorkspaceMembers");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.WorkspaceId, x.UserId }).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.WorkspaceId });
            entity.HasOne(x => x.WorkSpace)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany(x => x.WorkspaceMemberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasIndex(x => new { x.WorkspaceId, x.Name }).IsUnique();
            entity.HasIndex(x => new { x.WorkspaceId, x.CreatedAt });
            entity.HasIndex(x => new { x.WorkspaceId, x.Status });
            entity.HasIndex(x => x.Name).HasMethod("gin").HasOperators("gin_trgm_ops");
            entity.HasIndex(x => x.Description).HasMethod("gin").HasOperators("gin_trgm_ops");
            entity.HasOne(x => x.WorkSpace)
                .WithMany(x => x.Projects)
                .HasForeignKey(x => x.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.ToTable("ProjectMembers");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ProjectId, x.UserId }).IsUnique();
            entity.Property(x => x.Role).HasConversion<int>();
            entity.HasOne(x => x.Project)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany(x => x.ProjectMemberships)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("TaskItems");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.Priority).HasConversion<int>();
            entity.HasIndex(x => new { x.ProjectId, x.CreatedAt });
            entity.HasIndex(x => new { x.ProjectId, x.Status });
            entity.HasIndex(x => new { x.ProjectId, x.Priority });
            entity.HasIndex(x => new { x.ProjectId, x.AssignedUserId });
            entity.HasIndex(x => new { x.ProjectId, x.DueDate });
            entity.HasIndex(x => x.Title).HasMethod("gin").HasOperators("gin_trgm_ops");
            entity.HasIndex(x => x.Description).HasMethod("gin").HasOperators("gin_trgm_ops");
            entity.HasOne(x => x.Project)
                .WithMany(x => x.Tasks)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.AssignedUser)
                .WithMany(x => x.AssignedTasks)
                .HasForeignKey(x => x.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.CreatedByUser)
                .WithMany(x => x.CreatedTasks)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Comment>(entity =>
        {
            entity.ToTable("Comments");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Content).IsRequired().HasMaxLength(4000);
            entity.HasOne(x => x.TaskItem)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User)
                .WithMany(x => x.Comments)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Label>(entity =>
        {
            entity.ToTable("Labels");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Color).HasMaxLength(50);
            entity.HasIndex(x => new { x.ProjectId, x.Name }).IsUnique();
            entity.HasOne(x => x.Project)
                .WithMany(x => x.Labels)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskLabel>(entity =>
        {
            entity.ToTable("TaskLabels");
            entity.HasKey(x => new { x.TaskId, x.LabelId });
            entity.HasOne(x => x.TaskItem)
                .WithMany(x => x.TaskLabels)
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Label)
                .WithMany(x => x.TaskLabels)
                .HasForeignKey(x => x.LabelId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Type).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Title).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Message).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.IsRead).HasDefaultValue(false);
            entity.HasIndex(x => new { x.UserId, x.CreatedAt });
            entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
            entity.HasIndex(x => new { x.UserId, x.Type, x.CreatedAt });
            entity.HasOne(x => x.User)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.UserId, x.SourceEventId })
                .IsUnique()
                .HasFilter("\"SourceEventId\" IS NOT NULL");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EntityName).IsRequired().HasMaxLength(200);
            entity.Property(x => x.Action).IsRequired().HasMaxLength(100);
            entity.HasOne(x => x.User)
                .WithMany(x => x.AuditLogs)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AiSuggestion>(entity =>
        {
            entity.ToTable("AiSuggestions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.InputText).IsRequired();
            entity.Property(x => x.Status).IsRequired().HasMaxLength(50);
            entity.HasOne(x => x.TaskItem)
                .WithMany()
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Project)
                .WithMany(x => x.AiSuggestions)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.RequestedByUser)
                .WithMany(x => x.AiSuggestions)
                .HasForeignKey(x => x.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(256);
            entity.Property(x => x.ReplacedByTokenHash).HasMaxLength(256);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.RevokedAtUtc, x.ExpiresAtUtc });
            entity.HasOne(x => x.User)
                .WithMany(x => x.RefreshTokens)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
