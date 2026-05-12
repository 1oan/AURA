using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.Configurations;

public class StudentEmbeddingConfiguration : IEntityTypeConfiguration<StudentEmbedding>
{
    public void Configure(EntityTypeBuilder<StudentEmbedding> builder)
    {
        builder.ToTable("StudentEmbeddings");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.UserId).IsUnique();
        builder.Property(e => e.Embedding).HasColumnType("vector(384)");
        // No IVFFlat index here — Spec 4 adds it after first batch populates the column.
    }
}
