namespace Aura.Infrastructure.Persistence.Configurations;

using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class AllocationPeriodConfiguration : IEntityTypeConfiguration<AllocationPeriod>
{
    public void Configure(EntityTypeBuilder<AllocationPeriod> builder)
    {
        builder.HasKey(ap => ap.Id);

        builder.Property(ap => ap.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(ap => ap.Name)
            .IsUnique();

        builder.Property(ap => ap.StartDate)
            .IsRequired();

        builder.Property(ap => ap.EndDate)
            .IsRequired();

        builder.Property(ap => ap.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(AllocationPeriodStatus.Draft);

        builder.Property(ap => ap.Round1Date).IsRequired();
        builder.Property(ap => ap.ResponseWindowDays).IsRequired();
    }
}
