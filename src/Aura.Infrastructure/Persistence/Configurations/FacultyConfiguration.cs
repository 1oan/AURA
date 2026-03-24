namespace Aura.Infrastructure.Persistence.Configurations;

using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FacultyConfiguration : IEntityTypeConfiguration<Faculty>
{
    public void Configure(EntityTypeBuilder<Faculty> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(f => f.Name)
            .IsUnique();

        builder.Property(f => f.Abbreviation)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(f => f.Abbreviation)
            .IsUnique();
    }
}
