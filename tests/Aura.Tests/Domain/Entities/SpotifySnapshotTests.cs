using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class SpotifySnapshotTests
{
    [Fact]
    public void Create_ValidArgs_StoresAllFields()
    {
        var userId = Guid.NewGuid();
        var artists = new[] { "Radiohead", "Tame Impala" };
        var tracks = new[] { "Radiohead - Reckoner" };
        var genres = new[] { "indie rock", "psychedelic" };

        var snapshot = SpotifySnapshot.Create(userId, artists, tracks, genres, 0.65m, 0.4m, 0.55m);

        snapshot.UserId.Should().Be(userId);
        snapshot.TopArtists.Should().Equal(artists);
        snapshot.TopTracks.Should().Equal(tracks);
        snapshot.TopGenres.Should().Equal(genres);
        snapshot.AvgEnergy.Should().Be(0.65m);
        snapshot.AvgValence.Should().Be(0.4m);
        snapshot.AvgDanceability.Should().Be(0.55m);
        snapshot.FetchedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_NullAudioFeatures_Allowed()
    {
        var snapshot = SpotifySnapshot.Create(
            Guid.NewGuid(),
            Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
            null, null, null);

        snapshot.AvgEnergy.Should().BeNull();
        snapshot.AvgValence.Should().BeNull();
        snapshot.AvgDanceability.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyUserId_Throws()
    {
        var act = () => SpotifySnapshot.Create(
            Guid.Empty,
            Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(),
            null, null, null);
        act.Should().Throw<DomainException>();
    }
}
