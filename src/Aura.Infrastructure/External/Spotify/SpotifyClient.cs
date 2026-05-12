using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using Aura.Application.Common.Interfaces;
using Aura.Application.Common.Settings;
using Microsoft.Extensions.Options;

namespace Aura.Infrastructure.External.Spotify;

public class SpotifyClient(HttpClient httpClient, IOptions<SpotifySettings> options) : ISpotifyClient
{
    private readonly SpotifySettings _settings = options.Value;

    public async Task<SpotifyTokenResponse> ExchangeCodeAsync(
        string code, string redirectUri, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
        });

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<TokenPayload>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Spotify token endpoint returned empty body.");

        var scopes = string.IsNullOrWhiteSpace(payload.Scope)
            ? Array.Empty<string>()
            : payload.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return new SpotifyTokenResponse(payload.AccessToken, payload.RefreshToken, payload.ExpiresIn, scopes);
    }

    public async Task<SpotifyUserSnapshot> FetchUserSnapshotAsync(
        string accessToken, CancellationToken cancellationToken)
    {
        var topArtists = await GetTopAsync<ArtistsPayload>("https://api.spotify.com/v1/me/top/artists?limit=20", accessToken, cancellationToken);
        var topTracks = await GetTopAsync<TracksPayload>("https://api.spotify.com/v1/me/top/tracks?limit=20", accessToken, cancellationToken);

        var artists = topArtists?.Items?.Select(a => a.Name ?? "").Where(n => n.Length > 0).ToArray() ?? Array.Empty<string>();
        var tracks = topTracks?.Items?
            .Select(t => $"{t.Artists?.FirstOrDefault()?.Name ?? "Unknown"} - {t.Name}")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray() ?? Array.Empty<string>();
        var genres = topArtists?.Items?
            .SelectMany(a => a.Genres ?? Array.Empty<string>())
            .GroupBy(g => g)
            .OrderByDescending(g => g.Count())
            .Take(15)
            .Select(g => g.Key)
            .ToArray() ?? Array.Empty<string>();

        // Audio features: optional enrichment. If the request fails, return null aggregates rather than crash.
        decimal? avgEnergy = null, avgValence = null, avgDanceability = null;
        if (topTracks?.Items?.Length > 0)
        {
            var ids = string.Join(",", topTracks.Items.Where(t => !string.IsNullOrEmpty(t.Id)).Select(t => t.Id));
            try
            {
                var features = await GetTopAsync<AudioFeaturesPayload>(
                    $"https://api.spotify.com/v1/audio-features?ids={ids}", accessToken, cancellationToken);
                if (features?.AudioFeatures?.Length > 0)
                {
                    avgEnergy = (decimal)features.AudioFeatures.Average(f => f.Energy);
                    avgValence = (decimal)features.AudioFeatures.Average(f => f.Valence);
                    avgDanceability = (decimal)features.AudioFeatures.Average(f => f.Danceability);
                }
            }
            catch (HttpRequestException)
            {
                // Audio features are best-effort. Snapshot is still useful without them.
            }
        }

        return new SpotifyUserSnapshot(artists, tracks, genres, avgEnergy, avgValence, avgDanceability);
    }

    private async Task<T?> GetTopAsync<T>(string url, string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private sealed class TokenPayload
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; init; } = "";
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; init; } = "";
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }
        [JsonPropertyName("scope")] public string Scope { get; init; } = "";
    }
    private sealed class ArtistsPayload { public ArtistItem[]? Items { get; init; } }
    private sealed class ArtistItem { public string? Name { get; init; } public string[]? Genres { get; init; } }
    private sealed class TracksPayload { public TrackItem[]? Items { get; init; } }
    private sealed class TrackItem { public string? Id { get; init; } public string? Name { get; init; } public TrackArtist[]? Artists { get; init; } }
    private sealed class TrackArtist { public string? Name { get; init; } }
    private sealed class AudioFeaturesPayload
    {
        [JsonPropertyName("audio_features")] public AudioFeatureItem[]? AudioFeatures { get; init; }
    }
    private sealed class AudioFeatureItem { public double Energy { get; init; } public double Valence { get; init; } public double Danceability { get; init; } }
}
