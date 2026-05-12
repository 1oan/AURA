using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.Configurations;

public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("StudentProfiles", t =>
            t.HasCheckConstraint(
                "CK_StudentProfile_Cleanliness_Range",
                "\"CleanlinessLevel\" IS NULL OR \"CleanlinessLevel\" BETWEEN 1 AND 5"));

        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.UserId).IsUnique();

        builder.Property(p => p.SleepSchedule).HasConversion<int?>();
        builder.Property(p => p.WakeUpTime).HasConversion<int?>();
        builder.Property(p => p.NoiseTolerance).HasConversion<int?>();
        builder.Property(p => p.StudyLocation).HasConversion<int?>();
        builder.Property(p => p.GuestFrequency).HasConversion<int?>();
        builder.Property(p => p.SmokingHabit).HasConversion<int?>();

        builder.Property(p => p.TipiAnswers).HasColumnType("jsonb");
        builder.Property(p => p.InterestSlugs).HasColumnType("jsonb");
        builder.Property(p => p.SpotifyScopes).HasColumnType("jsonb");

        builder.Property(p => p.SpotifyAccessToken).HasMaxLength(2048);
        builder.Property(p => p.SpotifyRefreshToken).HasMaxLength(2048);
    }
}
