using Aura.Domain.Exceptions;

namespace Aura.Domain.Entities;

public class SpotifySnapshot
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime FetchedAt { get; private set; }
    public string[] TopArtists { get; private set; } = Array.Empty<string>();
    public string[] TopTracks { get; private set; } = Array.Empty<string>();
    public string[] TopGenres { get; private set; } = Array.Empty<string>();
    public decimal? AvgEnergy { get; private set; }
    public decimal? AvgValence { get; private set; }
    public decimal? AvgDanceability { get; private set; }

    private SpotifySnapshot() { }

    public static SpotifySnapshot Create(
        Guid userId,
        string[] topArtists, string[] topTracks, string[] topGenres,
        decimal? avgEnergy, decimal? avgValence, decimal? avgDanceability)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User id is required.");
        ArgumentNullException.ThrowIfNull(topArtists);
        ArgumentNullException.ThrowIfNull(topTracks);
        ArgumentNullException.ThrowIfNull(topGenres);

        return new SpotifySnapshot
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FetchedAt = DateTime.UtcNow,
            TopArtists = topArtists,
            TopTracks = topTracks,
            TopGenres = topGenres,
            AvgEnergy = avgEnergy,
            AvgValence = avgValence,
            AvgDanceability = avgDanceability,
        };
    }
}
