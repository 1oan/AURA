using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

    private class TokenPayload
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = "";
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("scope")] public string Scope { get; set; } = "";
    }
    private class ArtistsPayload { public ArtistItem[]? Items { get; set; } }
    private class ArtistItem { public string? Name { get; set; } public string[]? Genres { get; set; } }
    private class TracksPayload { public TrackItem[]? Items { get; set; } }
    private class TrackItem { public string? Id { get; set; } public string? Name { get; set; } public TrackArtist[]? Artists { get; set; } }
    private class TrackArtist { public string? Name { get; set; } }
    private class AudioFeaturesPayload
    {
        [JsonPropertyName("audio_features")] public AudioFeatureItem[]? AudioFeatures { get; set; }
    }
    private class AudioFeatureItem { public double Energy { get; set; } public double Valence { get; set; } public double Danceability { get; set; } }
}
