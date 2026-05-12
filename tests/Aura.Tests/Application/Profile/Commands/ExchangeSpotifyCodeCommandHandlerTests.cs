using System.Text;
using Aura.Application.Common.Interfaces;
using Aura.Application.Profile.Commands.ExchangeSpotifyCode;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using NSubstitute;

namespace Aura.Tests.Application.Profile.Commands;

public class ExchangeSpotifyCodeCommandHandlerTests
{
    private readonly IDataProtectionProvider _protectionProvider = Substitute.For<IDataProtectionProvider>();
    private readonly IDataProtector _stateProtector = Substitute.For<IDataProtector>();
    private readonly IDataProtector _tokenProtector = Substitute.For<IDataProtector>();
    private readonly IStudentProfileRepository _profiles = Substitute.For<IStudentProfileRepository>();
    private readonly IStudentEmbeddingRepository _embeddings = Substitute.For<IStudentEmbeddingRepository>();
    private readonly ISpotifySnapshotRepository _snapshots = Substitute.For<ISpotifySnapshotRepository>();
    private readonly ISpotifyClient _spotifyClient = Substitute.For<ISpotifyClient>();
    private readonly Guid _userId = Guid.NewGuid();

    public ExchangeSpotifyCodeCommandHandlerTests()
    {
        _protectionProvider.CreateProtector("Spotify.OAuth.State").Returns(_stateProtector);
        _protectionProvider.CreateProtector("Spotify.Tokens").Returns(_tokenProtector);
        _tokenProtector.Protect(Arg.Any<byte[]>()).Returns(c => c.Arg<byte[]>().Reverse().ToArray());
        _tokenProtector.Unprotect(Arg.Any<byte[]>()).Returns(c => c.Arg<byte[]>().Reverse().ToArray());
    }

    private ExchangeSpotifyCodeCommandHandler Create() =>
        new(_protectionProvider, _profiles, _embeddings, _snapshots, _spotifyClient);

    private static string EncodeState(string raw) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));

    [Fact]
    public async Task Handle_ValidState_ExchangesAndPersists()
    {
        _stateProtector.Unprotect(Arg.Any<byte[]>())
            .Returns(Encoding.UTF8.GetBytes(_userId.ToString()));
        _spotifyClient.ExchangeCodeAsync("code-abc", "https://r/", Arg.Any<CancellationToken>())
            .Returns(new SpotifyTokenResponse("access-1", "refresh-1", 3600, new[] { "user-top-read" }));
        _spotifyClient.FetchUserSnapshotAsync("access-1", Arg.Any<CancellationToken>())
            .Returns(new SpotifyUserSnapshot(
                new[] { "Radiohead" }, new[] { "Radiohead - Reckoner" }, new[] { "indie" },
                0.5m, 0.4m, 0.5m));
        _profiles.FindByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((StudentProfile?)null);

        await Create().Handle(
            new ExchangeSpotifyCodeCommand("code-abc", "https://r/", EncodeState("opaque-state")),
            CancellationToken.None);

        await _profiles.Received(1).AddAsync(
            Arg.Is<StudentProfile>(p => p.UserId == _userId && p.SpotifyConnected),
            Arg.Any<CancellationToken>());
        await _snapshots.Received(1).AddAsync(
            Arg.Is<SpotifySnapshot>(s => s.UserId == _userId && s.TopArtists.Length == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InvalidState_Throws()
    {
        _stateProtector.Unprotect(Arg.Any<byte[]>())
            .Returns(_ => throw new System.Security.Cryptography.CryptographicException());

        var act = async () => await Create().Handle(
            new ExchangeSpotifyCodeCommand("code", "https://r/", EncodeState("bad-state")),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*state*");
    }

    [Fact]
    public async Task Handle_StateUnprotectsToNonGuid_Throws()
    {
        _stateProtector.Unprotect(Arg.Any<byte[]>())
            .Returns(Encoding.UTF8.GetBytes("not-a-guid"));

        var act = async () => await Create().Handle(
            new ExchangeSpotifyCodeCommand("code", "https://r/", EncodeState("malformed")),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_SpotifyApiFailure_BubblesException()
    {
        _stateProtector.Unprotect(Arg.Any<byte[]>())
            .Returns(Encoding.UTF8.GetBytes(_userId.ToString()));
        _spotifyClient.ExchangeCodeAsync("code", "https://r/", Arg.Any<CancellationToken>())
            .Returns<Task<SpotifyTokenResponse>>(_ => throw new HttpRequestException("Spotify down"));

        var act = async () => await Create().Handle(
            new ExchangeSpotifyCodeCommand("code", "https://r/", EncodeState("signed-state")),
            CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        await _profiles.DidNotReceive().AddAsync(Arg.Any<StudentProfile>(), Arg.Any<CancellationToken>());
    }
}
