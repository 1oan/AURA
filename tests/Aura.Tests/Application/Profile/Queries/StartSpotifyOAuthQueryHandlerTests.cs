using System.Text;
using Aura.Application.Common.Interfaces;
using Aura.Application.Common.Settings;
using Aura.Application.Profile.Queries.StartSpotifyOAuth;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Aura.Tests.Application.Profile.Queries;

public class StartSpotifyOAuthQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IDataProtectionProvider _provider = Substitute.For<IDataProtectionProvider>();
    private readonly IDataProtector _protector = Substitute.For<IDataProtector>();
    private readonly Guid _userId = Guid.NewGuid();

    public StartSpotifyOAuthQueryHandlerTests()
    {
        _provider.CreateProtector("Spotify.OAuth.State").Returns(_protector);
        // The handler will call Protect(byte[]) with the UTF8 bytes of the user id.
        // We return a stable byte payload so we can assert the URL contains its base64url form.
        _protector.Protect(Arg.Any<byte[]>())
            .Returns(Encoding.UTF8.GetBytes("signed-state-value"));
    }

    [Fact]
    public async Task Handle_ReturnsAuthUrlContainingClientIdScopeRedirectAndState()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        var settings = Options.Create(new SpotifySettings
        {
            ClientId = "abc123",
            ClientSecret = "secret",
            RedirectUri = "http://127.0.0.1:5000/api/profile/spotify/callback",
            FrontendBaseUrl = "http://localhost:5173",
        });

        var handler = new StartSpotifyOAuthQueryHandler(_currentUser, _provider, settings);
        var result = await handler.Handle(new StartSpotifyOAuthQuery(), CancellationToken.None);

        var expectedState = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("signed-state-value"));

        result.AuthorizationUrl.Should().Contain("client_id=abc123");
        result.AuthorizationUrl.Should().Contain("scope=user-top-read");
        result.AuthorizationUrl.Should().Contain("redirect_uri=");
        result.AuthorizationUrl.Should().Contain($"state={expectedState}");
        result.AuthorizationUrl.Should().StartWith("https://accounts.spotify.com/authorize");
    }
}
