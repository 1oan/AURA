namespace Aura.Infrastructure.Persistence.Configurations;

using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class DormitoryConfiguration : IEntityTypeConfiguration<Dormitory>
{
    public void Configure(EntityTypeBuilder<Dormitory> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(d => d.Rooms)
            .WithOne(r => r.Dormitory)
            .HasForeignKey(r => r.DormitoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
