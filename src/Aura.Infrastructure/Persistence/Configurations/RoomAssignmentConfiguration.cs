using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.Configurations;

public class RoomAssignmentConfiguration : IEntityTypeConfiguration<RoomAssignment>
{
    public void Configure(EntityTypeBuilder<RoomAssignment> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasIndex(r => new { r.UserId, r.AllocationPeriodId })
            .IsUnique()
            .HasDatabaseName("IX_RoomAssignments_UserId_AllocationPeriodId");

        builder.HasIndex(r => new { r.RoomId, r.AllocationPeriodId })
            .HasDatabaseName("IX_RoomAssignments_RoomId_AllocationPeriodId");

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Room)
            .WithMany()
            .HasForeignKey(r => r.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.AllocationPeriod)
            .WithMany()
            .HasForeignKey(r => r.AllocationPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.RoommateGroup)
            .WithMany()
            .HasForeignKey(r => r.RoommateGroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(r => r.AssignedAt).IsRequired();
    }
}
