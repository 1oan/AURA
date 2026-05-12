using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.Configurations;

public class RoommateGroupConfiguration : IEntityTypeConfiguration<RoommateGroup>
{
    public void Configure(EntityTypeBuilder<RoommateGroup> builder)
    {
        builder.ToTable("RoommateGroups");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.RoomSizePreference).HasConversion<int>();
        builder.Property(g => g.Status).HasConversion<int>();
        builder.HasIndex(g => new { g.AllocationPeriodId, g.Status });

        builder.OwnsMany(g => g.Members, m =>
        {
            m.ToTable("GroupMembers");
            m.HasKey(gm => new { gm.GroupId, gm.UserId });
            m.WithOwner().HasForeignKey(gm => gm.GroupId);
        });

        builder.Ignore(g => g.DomainEvents);
    }
}
