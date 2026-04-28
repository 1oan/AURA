using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.Configurations;

public class DormAllocationConfiguration : IEntityTypeConfiguration<DormAllocation>
{
    public void Configure(EntityTypeBuilder<DormAllocation> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Round).IsRequired();
        builder.Property(a => a.AllocatedAt).IsRequired();
        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.Dormitory).WithMany().HasForeignKey(a => a.DormitoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(a => a.AllocationPeriod).WithMany().HasForeignKey(a => a.AllocationPeriodId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.UserId, a.AllocationPeriodId })
            .HasFilter($"\"Status\" IN ('{nameof(AllocationStatus.Pending)}', '{nameof(AllocationStatus.Accepted)}')")
            .IsUnique()
            .HasDatabaseName("IX_DormAllocation_ActiveOnePerUserPeriod");

        builder.HasIndex(a => a.AllocationPeriodId);
        builder.HasIndex(a => new { a.AllocationPeriodId, a.Round });
    }
}
