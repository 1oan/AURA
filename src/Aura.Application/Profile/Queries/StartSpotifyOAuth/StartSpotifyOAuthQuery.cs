using System.Text;
using System.Web;
using Aura.Application.Common.Interfaces;
using Aura.Application.Common.Settings;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Aura.Application.Profile.Queries.StartSpotifyOAuth;

public record StartSpotifyOAuthQuery : IRequest<StartSpotifyOAuthResult>;
public record StartSpotifyOAuthResult(string AuthorizationUrl);

public class StartSpotifyOAuthQueryHandler(
    ICurrentUserService currentUser,
    IDataProtectionProvider protectionProvider,
    IOptions<SpotifySettings> settings) : IRequestHandler<StartSpotifyOAuthQuery, StartSpotifyOAuthResult>
{
    public Task<StartSpotifyOAuthResult> Handle(StartSpotifyOAuthQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        var protector = protectionProvider.CreateProtector("Spotify.OAuth.State");
        var userIdBytes = Encoding.UTF8.GetBytes(userId.ToString());
        var protectedBytes = protector.Protect(userIdBytes);
        var state = WebEncoders.Base64UrlEncode(protectedBytes);

        var s = settings.Value;
        var url = "https://accounts.spotify.com/authorize" +
                  $"?client_id={HttpUtility.UrlEncode(s.ClientId)}" +
                  "&response_type=code" +
                  $"&redirect_uri={HttpUtility.UrlEncode(s.RedirectUri)}" +
                  "&scope=user-top-read" +
                  $"&state={HttpUtility.UrlEncode(state)}";

        return Task.FromResult(new StartSpotifyOAuthResult(url));
    }
}
