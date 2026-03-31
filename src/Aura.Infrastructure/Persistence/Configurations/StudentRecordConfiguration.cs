using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.Configurations;

public class StudentRecordConfiguration : IEntityTypeConfiguration<StudentRecord>
{
    public void Configure(EntityTypeBuilder<StudentRecord> builder)
    {
        builder.HasKey(sr => sr.Id);

        builder.Property(sr => sr.MatriculationCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(sr => sr.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sr => sr.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sr => sr.Points)
            .IsRequired();

        builder.HasOne(sr => sr.Faculty)
            .WithMany()
            .HasForeignKey(sr => sr.FacultyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sr => sr.AllocationPeriod)
            .WithMany()
            .HasForeignKey(sr => sr.AllocationPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sr => sr.User)
            .WithMany()
            .HasForeignKey(sr => sr.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(sr => new { sr.MatriculationCode, sr.AllocationPeriodId })
            .IsUnique();

        builder.HasIndex(sr => new { sr.UserId, sr.AllocationPeriodId })
            .IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL");
    }
}
