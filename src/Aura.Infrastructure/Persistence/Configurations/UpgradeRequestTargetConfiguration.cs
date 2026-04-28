using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.Configurations;

public class UpgradeRequestTargetConfiguration : IEntityTypeConfiguration<UpgradeRequestTarget>
{
    public void Configure(EntityTypeBuilder<UpgradeRequestTarget> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Rank).IsRequired();
        builder.HasOne(t => t.Dormitory).WithMany().HasForeignKey(t => t.DormitoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => new { t.UpgradeRequestId, t.Rank }).IsUnique();
        builder.HasIndex(t => new { t.UpgradeRequestId, t.DormitoryId }).IsUnique();
    }
}
