namespace Aura.Infrastructure.Persistence.Configurations;

using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class CampusConfiguration : IEntityTypeConfiguration<Campus>
{
    public void Configure(EntityTypeBuilder<Campus> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.HasMany(c => c.Dormitories)
            .WithOne(d => d.Campus)
            .HasForeignKey(d => d.CampusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
