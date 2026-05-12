using Aura.Application.Common.Interfaces;
using Aura.Application.Profile.Commands.DisconnectSpotify;
using Aura.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using NSubstitute;

namespace Aura.Tests.Application.Profile.Commands;

public class DisconnectSpotifyCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IStudentProfileRepository _profiles = Substitute.For<IStudentProfileRepository>();
    private readonly Guid _userId = Guid.NewGuid();

    private DisconnectSpotifyCommandHandler Create() => new(_currentUser, _profiles);

    [Fact]
    public async Task Handle_ConnectedProfile_ClearsTokens()
    {
        var protector = Substitute.For<IDataProtector>();
        protector.Protect(Arg.Any<byte[]>()).Returns(c => c.Arg<byte[]>());
        var profile = StudentProfile.Create(_userId);
        profile.ConnectSpotify("a", "b", DateTime.UtcNow.AddHours(1),
            new[] { "user-top-read" }, protector);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _profiles.FindByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(profile);

        await Create().Handle(new DisconnectSpotifyCommand(), CancellationToken.None);

        profile.SpotifyConnected.Should().BeFalse();
        await _profiles.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoProfile_NoOp()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _profiles.FindByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((StudentProfile?)null);

        await Create().Handle(new DisconnectSpotifyCommand(), CancellationToken.None);

        await _profiles.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
