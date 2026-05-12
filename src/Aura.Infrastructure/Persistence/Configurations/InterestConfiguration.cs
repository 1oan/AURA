using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.Configurations;

public class InterestConfiguration : IEntityTypeConfiguration<Interest>
{
    public void Configure(EntityTypeBuilder<Interest> builder)
    {
        builder.ToTable("Interests");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Slug).IsRequired().HasMaxLength(64);
        builder.HasIndex(i => i.Slug).IsUnique();
        builder.Property(i => i.Label).IsRequired().HasMaxLength(128);
        builder.Property(i => i.Category).IsRequired().HasMaxLength(32);
        builder.Property(i => i.DisplayOrder).IsRequired();
        builder.Property(i => i.IsActive).IsRequired();
        builder.Property(i => i.CreatedAt).IsRequired();
        builder.HasIndex(i => new { i.Category, i.DisplayOrder, i.IsActive });

        // Seed data with stable GUIDs (do NOT use Guid.NewGuid() in HasData — re-runs would churn).
        var seedTime = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        builder.HasData(
            // Sports
            Seed("a0000001-0001-0001-0001-000000000001", "football", "Football", "sports", 1),
            Seed("a0000001-0001-0001-0001-000000000002", "basketball", "Basketball", "sports", 2),
            Seed("a0000001-0001-0001-0001-000000000003", "tennis", "Tennis", "sports", 3),
            Seed("a0000001-0001-0001-0001-000000000004", "running", "Running", "sports", 4),
            Seed("a0000001-0001-0001-0001-000000000005", "gym", "Gym", "sports", 5),
            Seed("a0000001-0001-0001-0001-000000000006", "swimming", "Swimming", "sports", 6),
            Seed("a0000001-0001-0001-0001-000000000007", "hiking", "Hiking", "sports", 7),
            Seed("a0000001-0001-0001-0001-000000000008", "cycling", "Cycling", "sports", 8),
            // Entertainment
            Seed("a0000002-0002-0002-0002-000000000001", "movies", "Movies", "entertainment", 1),
            Seed("a0000002-0002-0002-0002-000000000002", "tv-series", "TV Series", "entertainment", 2),
            Seed("a0000002-0002-0002-0002-000000000003", "anime", "Anime", "entertainment", 3),
            Seed("a0000002-0002-0002-0002-000000000004", "gaming", "Gaming", "entertainment", 4),
            Seed("a0000002-0002-0002-0002-000000000005", "board-games", "Board Games", "entertainment", 5),
            Seed("a0000002-0002-0002-0002-000000000006", "podcasts", "Podcasts", "entertainment", 6),
            Seed("a0000002-0002-0002-0002-000000000007", "live-music", "Live Music", "entertainment", 7),
            Seed("a0000002-0002-0002-0002-000000000008", "reading", "Reading", "entertainment", 8),
            // Arts
            Seed("a0000003-0003-0003-0003-000000000001", "drawing", "Drawing", "arts", 1),
            Seed("a0000003-0003-0003-0003-000000000002", "photography", "Photography", "arts", 2),
            Seed("a0000003-0003-0003-0003-000000000003", "writing", "Writing", "arts", 3),
            Seed("a0000003-0003-0003-0003-000000000004", "music-playing", "Playing music", "arts", 4),
            Seed("a0000003-0003-0003-0003-000000000005", "theater", "Theater", "arts", 5),
            Seed("a0000003-0003-0003-0003-000000000006", "crafts", "Crafts", "arts", 6),
            // Academic
            Seed("a0000004-0004-0004-0004-000000000001", "programming", "Programming", "academic", 1),
            Seed("a0000004-0004-0004-0004-000000000002", "science", "Science", "academic", 2),
            Seed("a0000004-0004-0004-0004-000000000003", "history", "History", "academic", 3),
            Seed("a0000004-0004-0004-0004-000000000004", "languages", "Languages", "academic", 4),
            Seed("a0000004-0004-0004-0004-000000000005", "mathematics", "Mathematics", "academic", 5),
            // Lifestyle
            Seed("a0000005-0005-0005-0005-000000000001", "cooking", "Cooking", "lifestyle", 1),
            Seed("a0000005-0005-0005-0005-000000000002", "traveling", "Traveling", "lifestyle", 2),
            Seed("a0000005-0005-0005-0005-000000000003", "volunteering", "Volunteering", "lifestyle", 3),
            Seed("a0000005-0005-0005-0005-000000000004", "pets", "Pets", "lifestyle", 4),
            Seed("a0000005-0005-0005-0005-000000000005", "fashion", "Fashion", "lifestyle", 5)
        );

        object Seed(string id, string slug, string label, string category, int order) => new
        {
            Id = Guid.Parse(id),
            Slug = slug,
            Label = label,
            Category = category,
            DisplayOrder = order,
            IsActive = true,
            CreatedAt = seedTime,
        };
    }
}
