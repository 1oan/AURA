using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.Configurations;

public class GroupInvitationConfiguration : IEntityTypeConfiguration<GroupInvitation>
{
    public void Configure(EntityTypeBuilder<GroupInvitation> builder)
    {
        builder.ToTable("GroupInvitations");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Status).HasConversion<int>();
        builder.HasIndex(i => i.InviteeUserId);
        builder.HasIndex(i => i.GroupId);

        builder.HasIndex(i => new { i.GroupId, i.InviteeUserId })
            .IsUnique()
            .HasFilter("\"Status\" = 0");
    }
}
