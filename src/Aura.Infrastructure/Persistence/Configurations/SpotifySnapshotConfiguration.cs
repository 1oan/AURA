using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aura.Infrastructure.Persistence.Configurations;

public class SpotifySnapshotConfiguration : IEntityTypeConfiguration<SpotifySnapshot>
{
    public void Configure(EntityTypeBuilder<SpotifySnapshot> builder)
    {
        builder.ToTable("SpotifySnapshots");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => new { s.UserId, s.FetchedAt }).IsDescending(false, true);
        builder.Property(s => s.TopArtists).HasColumnType("jsonb");
        builder.Property(s => s.TopTracks).HasColumnType("jsonb");
        builder.Property(s => s.TopGenres).HasColumnType("jsonb");
    }
}
