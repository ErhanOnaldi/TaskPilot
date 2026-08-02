using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskPilot.Domain.Knowledge;
using TaskPilot.Domain.Entities;
using NpgsqlTypes;

namespace TaskPilot.Persistence.Features.Knowledge;

public sealed class KnowledgeFolderConfiguration : IEntityTypeConfiguration<KnowledgeFolder>
{
    public void Configure(EntityTypeBuilder<KnowledgeFolder> builder)
    {
        builder.ToTable("KnowledgeFolders"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired(); builder.Property(x => x.NormalizedSlug).HasMaxLength(160).IsRequired();
        builder.HasIndex(x => new { x.WorkspaceId, x.ProjectId, x.NormalizedSlug }).IsUnique().AreNullsDistinct(false);
        builder.HasIndex(x => new { x.WorkspaceId, x.ParentFolderId });
        builder.HasOne<WorkSpace>().WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<KnowledgeFolder>().WithMany().HasForeignKey(x => x.ParentFolderId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("KnowledgeNotes"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired(); builder.Property(x => x.NormalizedSlug).HasMaxLength(160).IsRequired(); builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property<NpgsqlTsVector>("SearchDocument")
            .HasComputedColumnSql("to_tsvector('simple', coalesce(\"Title\", '') || ' ' || coalesce(\"Content\", ''))", stored: true);
        builder.HasIndex(x => new { x.WorkspaceId, x.NormalizedSlug }).IsUnique();
        builder.HasIndex(x => new { x.WorkspaceId, x.ProjectId, x.UpdatedAt });
        builder.HasIndex("SearchDocument").HasMethod("GIN");
        builder.HasOne<WorkSpace>().WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<KnowledgeFolder>().WithMany().HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NoteRevisionConfiguration : IEntityTypeConfiguration<NoteRevision>
{
    public void Configure(EntityTypeBuilder<NoteRevision> builder)
    {
        builder.ToTable("KnowledgeNoteRevisions"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired(); builder.Property(x => x.NormalizedSlug).HasMaxLength(160).IsRequired(); builder.Property(x => x.Content).IsRequired();
        builder.HasIndex(x => new { x.NoteId, x.RevisionNumber }).IsUnique();
        builder.HasOne(x => x.Note).WithMany().HasForeignKey(x => x.NoteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<NoteRevision>().WithMany().HasForeignKey(x => x.RestoredFromRevisionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NoteLinkConfiguration : IEntityTypeConfiguration<NoteLink>
{
    public void Configure(EntityTypeBuilder<NoteLink> builder)
    {
        builder.ToTable("KnowledgeNoteLinks"); builder.HasKey(x => x.Id);
        builder.Property(x => x.TargetNormalizedSlug).HasMaxLength(160).IsRequired(); builder.Property(x => x.Alias).HasMaxLength(200);
        builder.HasIndex(x => new { x.SourceNoteId, x.TargetNormalizedSlug }).IsUnique(); builder.HasIndex(x => new { x.WorkspaceId, x.TargetNoteId });
        builder.HasOne(x => x.SourceNote).WithMany().HasForeignKey(x => x.SourceNoteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.TargetNote).WithMany().HasForeignKey(x => x.TargetNoteId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<WorkSpace>().WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class KnowledgeTagConfiguration : IEntityTypeConfiguration<KnowledgeTag>
{
    public void Configure(EntityTypeBuilder<KnowledgeTag> builder)
    {
        builder.ToTable("KnowledgeTags"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired(); builder.Property(x => x.NormalizedSlug).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new { x.WorkspaceId, x.ProjectId, x.NormalizedSlug }).IsUnique().AreNullsDistinct(false);
        builder.HasOne<WorkSpace>().WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class NoteTagAssignmentConfiguration : IEntityTypeConfiguration<NoteTagAssignment>
{
    public void Configure(EntityTypeBuilder<NoteTagAssignment> builder)
    {
        builder.ToTable("KnowledgeNoteTagAssignments"); builder.HasKey(x => x.Id); builder.HasIndex(x => new { x.NoteId, x.KnowledgeTagId }).IsUnique();
        builder.HasOne<Note>().WithMany().HasForeignKey(x => x.NoteId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<KnowledgeTag>().WithMany().HasForeignKey(x => x.KnowledgeTagId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TaskNoteLinkConfiguration : IEntityTypeConfiguration<TaskNoteLink>
{
    public void Configure(EntityTypeBuilder<TaskNoteLink> builder)
    {
        builder.ToTable("TaskNoteLinks"); builder.HasKey(x => x.Id); builder.HasIndex(x => new { x.TaskId, x.NoteId }).IsUnique(); builder.HasIndex(x => new { x.WorkspaceId, x.TaskId });
        builder.HasOne<WorkSpace>().WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<TaskItem>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Note>().WithMany().HasForeignKey(x => x.NoteId).OnDelete(DeleteBehavior.Cascade);
    }
}
