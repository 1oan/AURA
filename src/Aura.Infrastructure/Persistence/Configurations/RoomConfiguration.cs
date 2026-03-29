namespace Aura.Infrastructure.Persistence.Configurations;

using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Number)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(r => r.Floor)
            .IsRequired();

        builder.Property(r => r.Capacity)
            .IsRequired();

        builder.Property(r => r.Gender)
            .IsRequired()
            .HasConversion<int>();

        // Room numbers must be unique within a dormitory
        builder.HasIndex(r => new { r.DormitoryId, r.Number })
            .IsUnique();
    }
}
