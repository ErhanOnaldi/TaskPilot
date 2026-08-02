using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskPilot.Domain.AI.Semantic;
using Pgvector;
using TaskPilot.Domain.Entities;

namespace TaskPilot.Persistence.Features.Semantic;

public sealed class SemanticDocumentConfiguration : IEntityTypeConfiguration<SemanticDocument>
{
    public void Configure(EntityTypeBuilder<SemanticDocument> builder)
    {
        builder.ToTable("SemanticDocuments"); builder.HasKey(x => x.Id);
        builder.Property(x => x.SourceText).IsRequired(); builder.Property(x => x.ContentHash).HasMaxLength(64).IsRequired(); builder.Property(x => x.EmbeddingModel).HasMaxLength(200).IsRequired(); builder.Property(x => x.SourceType).HasConversion<int>();
        builder.Property(x => x.UpdatedAt).IsConcurrencyToken();
        builder.Ignore(x => x.Embedding);
        builder.Property<Vector?>("EmbeddingVector").HasColumnName("Embedding").HasColumnType("vector(768)");
        builder.HasIndex(x => new { x.SourceType, x.SourceId, x.ChunkIndex, x.EmbeddingModel }).IsUnique(); builder.HasIndex(x => new { x.WorkspaceId, x.ProjectId, x.IsActive, x.UpdatedAt }); builder.HasAnnotation("Semantic:PgvectorHnswCosine", true);
        builder.HasIndex("EmbeddingVector").HasMethod("hnsw").HasOperators("vector_cosine_ops").HasStorageParameter("m", 16).HasStorageParameter("ef_construction", 64);
        builder.HasOne<WorkSpace>().WithMany().HasForeignKey(x => x.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Project>().WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
    }
}
