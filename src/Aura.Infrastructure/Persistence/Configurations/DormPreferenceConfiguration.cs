using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.Configurations;

public class DormPreferenceConfiguration : IEntityTypeConfiguration<DormPreference>
{
    public void Configure(EntityTypeBuilder<DormPreference> builder)
    {
        builder.HasKey(dp => dp.Id);

        builder.Property(dp => dp.Rank)
            .IsRequired();

        builder.Property(dp => dp.CreatedAt).IsRequired();

        builder.HasOne(dp => dp.User)
            .WithMany()
            .HasForeignKey(dp => dp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dp => dp.AllocationPeriod)
            .WithMany()
            .HasForeignKey(dp => dp.AllocationPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dp => dp.Dormitory)
            .WithMany()
            .HasForeignKey(dp => dp.DormitoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(dp => new { dp.UserId, dp.AllocationPeriodId, dp.DormitoryId })
            .IsUnique();

        builder.HasIndex(dp => new { dp.UserId, dp.AllocationPeriodId, dp.Rank })
            .IsUnique();
    }
}
