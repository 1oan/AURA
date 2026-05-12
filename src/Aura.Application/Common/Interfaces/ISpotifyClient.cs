namespace Aura.Application.Common.Interfaces;

public interface ISpotifyClient
{
    Task<SpotifyTokenResponse> ExchangeCodeAsync(
        string code, string redirectUri, CancellationToken cancellationToken);

    Task<SpotifyUserSnapshot> FetchUserSnapshotAsync(
        string accessToken, CancellationToken cancellationToken);
}

public record SpotifyTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    string[] Scopes);

public record SpotifyUserSnapshot(
    string[] TopArtists,
    string[] TopTracks,
    string[] TopGenres,
    decimal? AvgEnergy,
    decimal? AvgValence,
    decimal? AvgDanceability);
