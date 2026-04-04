using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.Configurations;

public class EmailConfirmationCodeConfiguration : IEntityTypeConfiguration<EmailConfirmationCode>
{
    public void Configure(EntityTypeBuilder<EmailConfirmationCode> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(6);

        builder.Property(c => c.ExpiresAt)
            .IsRequired();

        builder.Property(c => c.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.HasIndex(c => new { c.UserId, c.Code });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}