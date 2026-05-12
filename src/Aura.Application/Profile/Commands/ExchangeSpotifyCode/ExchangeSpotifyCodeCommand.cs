using System.Security.Cryptography;
using System.Text;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace Aura.Application.Profile.Commands.ExchangeSpotifyCode;

public record ExchangeSpotifyCodeCommand(string Code, string RedirectUri, string State) : IRequest;

public class ExchangeSpotifyCodeCommandHandler(
    IDataProtectionProvider protectionProvider,
    IStudentProfileRepository profileRepository,
    IStudentEmbeddingRepository embeddingRepository,
    ISpotifySnapshotRepository snapshotRepository,
    ISpotifyClient spotifyClient) : IRequestHandler<ExchangeSpotifyCodeCommand>
{
    public async Task Handle(ExchangeSpotifyCodeCommand request, CancellationToken cancellationToken)
    {
        var stateProtector = protectionProvider.CreateProtector("Spotify.OAuth.State");
        var tokenProtector = protectionProvider.CreateProtector("Spotify.Tokens");

        Guid userId;
        try
        {
            var protectedBytes = WebEncoders.Base64UrlDecode(request.State);
            var unprotectedBytes = stateProtector.Unprotect(protectedBytes);
            var unprotected = Encoding.UTF8.GetString(unprotectedBytes);
            if (!Guid.TryParse(unprotected, out userId))
                throw new DomainException("Spotify OAuth state is malformed.");
        }
        catch (CryptographicException)
        {
            throw new DomainException("Spotify OAuth state could not be verified.");
        }
        catch (FormatException)
        {
            throw new DomainException("Spotify OAuth state is malformed.");
        }

        var tokenResponse = await spotifyClient.ExchangeCodeAsync(request.Code, request.RedirectUri, cancellationToken);
        var snapshotData = await spotifyClient.FetchUserSnapshotAsync(tokenResponse.AccessToken, cancellationToken);

        var profile = await profileRepository.FindByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            profile = StudentProfile.Create(userId);
            await profileRepository.AddAsync(profile, cancellationToken);
            await embeddingRepository.AddAsync(StudentEmbedding.Create(userId), cancellationToken);
        }

        profile.ConnectSpotify(
            tokenResponse.AccessToken, tokenResponse.RefreshToken,
            DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresInSeconds),
            tokenResponse.Scopes, tokenProtector);

        var snapshot = SpotifySnapshot.Create(
            userId,
            snapshotData.TopArtists, snapshotData.TopTracks, snapshotData.TopGenres,
            snapshotData.AvgEnergy, snapshotData.AvgValence, snapshotData.AvgDanceability);
        await snapshotRepository.AddAsync(snapshot, cancellationToken);

        await profileRepository.SaveChangesAsync(cancellationToken);
    }
}
