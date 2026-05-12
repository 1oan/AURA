using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class StudentProfileTests
{
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Create_ValidUserId_ReturnsEmptyProfile()
    {
        var profile = StudentProfile.Create(_userId);

        profile.UserId.Should().Be(_userId);
        profile.LifestyleCompletedAt.Should().BeNull();
        profile.TipiCompletedAt.Should().BeNull();
        profile.InterestsCompletedAt.Should().BeNull();
        profile.SpotifyConnected.Should().BeFalse();
    }

    [Fact]
    public void Create_EmptyUserId_ThrowsDomainException()
    {
        var act = () => StudentProfile.Create(Guid.Empty);
        act.Should().Throw<DomainException>().WithMessage("*user*");
    }

    [Fact]
    public void SubmitLifestyle_ValidInputs_StoresFieldsAndStampsTimestamp()
    {
        var profile = StudentProfile.Create(_userId);

        profile.SubmitLifestyle(
            SleepSchedule.Late, WakeUpTime.Late, 4,
            NoiseTolerance.Some, StudyLocation.Mixed,
            GuestFrequency.Weekly, SmokingHabit.OutdoorsOnly);

        profile.SleepSchedule.Should().Be(SleepSchedule.Late);
        profile.WakeUpTime.Should().Be(WakeUpTime.Late);
        profile.CleanlinessLevel.Should().Be(4);
        profile.NoiseTolerance.Should().Be(NoiseTolerance.Some);
        profile.StudyLocation.Should().Be(StudyLocation.Mixed);
        profile.GuestFrequency.Should().Be(GuestFrequency.Weekly);
        profile.SmokingHabit.Should().Be(SmokingHabit.OutdoorsOnly);
        profile.LifestyleCompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void SubmitLifestyle_CleanlinessOutOfRange_ThrowsDomainException(int cleanliness)
    {
        var profile = StudentProfile.Create(_userId);

        var act = () => profile.SubmitLifestyle(
            SleepSchedule.Normal, WakeUpTime.Normal, cleanliness,
            NoiseTolerance.Some, StudyLocation.Mixed,
            GuestFrequency.Weekly, SmokingHabit.No);

        act.Should().Throw<DomainException>().WithMessage("*cleanliness*");
    }

    [Fact]
    public void SubmitLifestyle_InvalidEnum_ThrowsDomainException()
    {
        var profile = StudentProfile.Create(_userId);

        var act = () => profile.SubmitLifestyle(
            (SleepSchedule)99, WakeUpTime.Normal, 3,
            NoiseTolerance.Some, StudyLocation.Mixed,
            GuestFrequency.Weekly, SmokingHabit.No);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SubmitTipi_ValidAnswers_StoresRawAndComputesScores()
    {
        var profile = StudentProfile.Create(_userId);

        // Reference case from Gosling 2003: extraversion-high pattern
        // item1=7 (extraverted), item6=2 (NOT reserved) → reverse(2)=6 → (7+6)/2 = 6.5
        var answers = new[] { 7, 2, 6, 2, 5, 2, 5, 3, 6, 3 };

        profile.SubmitTipi(answers);

        profile.TipiAnswers.Should().Equal(answers);
        profile.TipiExtraversion.Should().Be(6.5m);
        profile.TipiAgreeableness.Should().Be(((8 - 2) + 5) / 2m);   // (6+5)/2 = 5.5
        profile.TipiConscientiousness.Should().Be((6 + (8 - 3)) / 2m); // (6+5)/2 = 5.5
        profile.TipiEmotionalStability.Should().Be(((8 - 2) + 6) / 2m); // (6+6)/2 = 6.0
        profile.TipiOpenness.Should().Be((5 + (8 - 3)) / 2m); // (5+5)/2 = 5.0
        profile.TipiCompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(9)]
    [InlineData(11)]
    public void SubmitTipi_WrongLength_ThrowsDomainException(int length)
    {
        var profile = StudentProfile.Create(_userId);
        var answers = Enumerable.Repeat(4, length).ToArray();

        var act = () => profile.SubmitTipi(answers);

        act.Should().Throw<DomainException>().WithMessage("*10*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    public void SubmitTipi_AnswerOutOfRange_ThrowsDomainException(int badValue)
    {
        var profile = StudentProfile.Create(_userId);
        var answers = new[] { 4, 4, 4, 4, 4, 4, 4, 4, 4, badValue };

        var act = () => profile.SubmitTipi(answers);

        act.Should().Throw<DomainException>().WithMessage("*1*7*");
    }

    [Fact]
    public void SubmitTipi_NullAnswers_ThrowsDomainException()
    {
        var profile = StudentProfile.Create(_userId);

        var act = () => profile.SubmitTipi(null!);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetInterests_ValidSlugs_StoresAndStampsTimestamp()
    {
        var profile = StudentProfile.Create(_userId);

        profile.SetInterests(new[] { "football", "gaming", "cooking" });

        profile.InterestSlugs.Should().Equal("football", "gaming", "cooking");
        profile.InterestsCompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void SetInterests_EmptyArray_StoresEmptyAndStampsTimestamp()
    {
        var profile = StudentProfile.Create(_userId);

        profile.SetInterests(Array.Empty<string>());

        profile.InterestSlugs.Should().BeEmpty();
        profile.InterestsCompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void SetInterests_Duplicates_ThrowsDomainException()
    {
        var profile = StudentProfile.Create(_userId);

        var act = () => profile.SetInterests(new[] { "football", "football", "gaming" });

        act.Should().Throw<DomainException>().WithMessage("*duplicate*");
    }

    [Fact]
    public void SetInterests_TooMany_ThrowsDomainException()
    {
        var profile = StudentProfile.Create(_userId);
        var slugs = Enumerable.Range(1, 51).Select(i => $"slug-{i}").ToArray();

        var act = () => profile.SetInterests(slugs);

        act.Should().Throw<DomainException>().WithMessage("*50*");
    }

    [Fact]
    public void SetInterests_BlankSlug_ThrowsDomainException()
    {
        var profile = StudentProfile.Create(_userId);

        var act = () => profile.SetInterests(new[] { "football", "" });

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ConnectSpotify_ValidArgs_StoresEncryptedTokensAndStamps()
    {
        var profile = StudentProfile.Create(_userId);
        var protector = new FakeDataProtector();

        profile.ConnectSpotify(
            "access-abc", "refresh-xyz",
            DateTime.UtcNow.AddHours(1),
            new[] { "user-top-read" },
            protector);

        profile.SpotifyConnected.Should().BeTrue();
        profile.SpotifyConnectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        profile.SpotifyAccessToken.Should().NotBe("access-abc"); // encrypted
        profile.SpotifyRefreshToken.Should().NotBe("refresh-xyz");
        profile.SpotifyScopes.Should().Equal("user-top-read");
    }

    [Fact]
    public void DisconnectSpotify_ClearsAllSpotifyFields()
    {
        var profile = StudentProfile.Create(_userId);
        profile.ConnectSpotify("a", "b", DateTime.UtcNow.AddHours(1),
            new[] { "user-top-read" }, new FakeDataProtector());

        profile.DisconnectSpotify();

        profile.SpotifyConnected.Should().BeFalse();
        profile.SpotifyConnectedAt.Should().BeNull();
        profile.SpotifyAccessToken.Should().BeNull();
        profile.SpotifyRefreshToken.Should().BeNull();
        profile.SpotifyTokenExpiresAt.Should().BeNull();
        profile.SpotifyScopes.Should().BeEmpty();
    }

    [Fact]
    public void CompletenessPercent_AllSectionsFilled_Returns100()
    {
        var profile = StudentProfile.Create(_userId);
        profile.SubmitLifestyle(SleepSchedule.Normal, WakeUpTime.Normal, 3,
            NoiseTolerance.Some, StudyLocation.Mixed, GuestFrequency.Weekly, SmokingHabit.No);
        profile.SubmitTipi(new[] { 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 });
        profile.SetInterests(new[] { "football" });
        profile.ConnectSpotify("a", "b", DateTime.UtcNow.AddHours(1),
            new[] { "user-top-read" }, new FakeDataProtector());

        profile.CompletenessPercent.Should().Be(100);
    }

    [Fact]
    public void CompletenessPercent_NothingFilled_Returns0()
    {
        var profile = StudentProfile.Create(_userId);
        profile.CompletenessPercent.Should().Be(0);
    }

    [Fact]
    public void CompletenessPercent_OnlyLifestyle_Returns25()
    {
        var profile = StudentProfile.Create(_userId);
        profile.SubmitLifestyle(SleepSchedule.Normal, WakeUpTime.Normal, 3,
            NoiseTolerance.Some, StudyLocation.Mixed, GuestFrequency.Weekly, SmokingHabit.No);

        profile.CompletenessPercent.Should().Be(25);
    }

    [Fact]
    public void HasLifestyleData_AfterSubmit_True()
    {
        var profile = StudentProfile.Create(_userId);
        profile.SubmitLifestyle(SleepSchedule.Normal, WakeUpTime.Normal, 3,
            NoiseTolerance.Some, StudyLocation.Mixed, GuestFrequency.Weekly, SmokingHabit.No);

        profile.HasLifestyleData.Should().BeTrue();
    }
}

internal class FakeDataProtector : Microsoft.AspNetCore.DataProtection.IDataProtector
{
    public Microsoft.AspNetCore.DataProtection.IDataProtector CreateProtector(string purpose) => this;
    public byte[] Protect(byte[] plaintext) => plaintext.Reverse().ToArray();
    public byte[] Unprotect(byte[] protectedData) => protectedData.Reverse().ToArray();
}
