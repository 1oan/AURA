using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.Configurations;

public class UpgradeRequestConfiguration : IEntityTypeConfiguration<UpgradeRequest>
{
    public void Configure(EntityTypeBuilder<UpgradeRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.IsActive).IsRequired();

        builder.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(r => r.AllocationPeriod).WithMany().HasForeignKey(r => r.AllocationPeriodId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.Targets)
            .WithOne(t => t.UpgradeRequest!)
            .HasForeignKey(t => t.UpgradeRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.UserId, r.AllocationPeriodId })
            .HasFilter("\"IsActive\" = true")
            .IsUnique()
            .HasDatabaseName("IX_UpgradeRequest_OneActivePerUserPeriod");
    }
}
