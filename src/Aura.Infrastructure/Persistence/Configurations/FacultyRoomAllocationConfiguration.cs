namespace Aura.Infrastructure.Persistence.Configurations;

using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class FacultyRoomAllocationConfiguration : IEntityTypeConfiguration<FacultyRoomAllocation>
{
    public void Configure(EntityTypeBuilder<FacultyRoomAllocation> builder)
    {
        builder.HasKey(fra => fra.Id);

        builder.HasOne(fra => fra.Faculty)
            .WithMany()
            .HasForeignKey(fra => fra.FacultyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fra => fra.Room)
            .WithMany()
            .HasForeignKey(fra => fra.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fra => fra.AllocationPeriod)
            .WithMany()
            .HasForeignKey(fra => fra.AllocationPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        // A room can only be assigned to one faculty per allocation period
        builder.HasIndex(fra => new { fra.RoomId, fra.AllocationPeriodId })
            .IsUnique();
    }
}
