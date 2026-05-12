using Aura.Application.Common.Interfaces;
using Aura.Application.Profile.Queries.GetMyProfile;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using NSubstitute;

namespace Aura.Tests.Application.Profile.Queries;

public class GetMyProfileQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IStudentProfileRepository _profiles = Substitute.For<IStudentProfileRepository>();
    private readonly Guid _userId = Guid.NewGuid();

    private GetMyProfileQueryHandler Create() => new(_currentUser, _profiles);

    [Fact]
    public async Task Handle_NoProfile_ReturnsEmptyDto()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _profiles.FindByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((StudentProfile?)null);

        var dto = await Create().Handle(new GetMyProfileQuery(), CancellationToken.None);

        dto.CompletenessPercent.Should().Be(0);
        dto.HasLifestyleData.Should().BeFalse();
        dto.Lifestyle.Should().BeNull();
        dto.Tipi.Should().BeNull();
        dto.Interests.Slugs.Should().BeEmpty();
        dto.Spotify.Connected.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_FullyFilledProfile_Returns100Percent()
    {
        var protector = Substitute.For<IDataProtector>();
        protector.Protect(Arg.Any<byte[]>()).Returns(c => c.Arg<byte[]>());

        var profile = StudentProfile.Create(_userId);
        profile.SubmitLifestyle(SleepSchedule.Late, WakeUpTime.Late, 3,
            NoiseTolerance.Some, StudyLocation.Mixed, GuestFrequency.Weekly, SmokingHabit.No);
        profile.SubmitTipi(new[] { 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 });
        profile.SetInterests(new[] { "football" });
        profile.ConnectSpotify("a", "b", DateTime.UtcNow.AddHours(1),
            new[] { "user-top-read" }, protector);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _profiles.FindByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(profile);

        var dto = await Create().Handle(new GetMyProfileQuery(), CancellationToken.None);

        dto.CompletenessPercent.Should().Be(100);
        dto.Lifestyle.Should().NotBeNull();
        dto.Tipi.Should().NotBeNull();
        dto.Interests.Slugs.Should().Equal("football");
        dto.Spotify.Connected.Should().BeTrue();
    }
}
