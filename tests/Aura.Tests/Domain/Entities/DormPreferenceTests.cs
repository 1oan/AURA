using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class DormPreferenceTests
{
    private static readonly Guid ValidUserId = Guid.NewGuid();
    private static readonly Guid ValidPeriodId = Guid.NewGuid();
    private static readonly Guid ValidDormId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidInputs_ReturnsPopulatedPreference()
    {
        var preference = DormPreference.Create(ValidUserId, ValidPeriodId, ValidDormId, rank: 1);

        preference.Id.Should().NotBe(Guid.Empty);
        preference.UserId.Should().Be(ValidUserId);
        preference.AllocationPeriodId.Should().Be(ValidPeriodId);
        preference.DormitoryId.Should().Be(ValidDormId);
        preference.Rank.Should().Be(1);
    }

    [Fact]
    public void Create_WithEmptyUserId_ThrowsDomainException()
    {
        var act = () => DormPreference.Create(Guid.Empty, ValidPeriodId, ValidDormId, 1);

        act.Should().Throw<DomainException>().WithMessage("User ID is required.");
    }

    [Fact]
    public void Create_WithEmptyAllocationPeriodId_ThrowsDomainException()
    {
        var act = () => DormPreference.Create(ValidUserId, Guid.Empty, ValidDormId, 1);

        act.Should().Throw<DomainException>().WithMessage("Allocation period ID is required.");
    }

    [Fact]
    public void Create_WithEmptyDormitoryId_ThrowsDomainException()
    {
        var act = () => DormPreference.Create(ValidUserId, ValidPeriodId, Guid.Empty, 1);

        act.Should().Throw<DomainException>().WithMessage("Dormitory ID is required.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithRankLessThanOne_ThrowsDomainException(int rank)
    {
        var act = () => DormPreference.Create(ValidUserId, ValidPeriodId, ValidDormId, rank);

        act.Should().Throw<DomainException>().WithMessage("Rank must be at least 1.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(15)]
    public void Create_WithPositiveRank_Succeeds(int rank)
    {
        var preference = DormPreference.Create(ValidUserId, ValidPeriodId, ValidDormId, rank);

        preference.Rank.Should().Be(rank);
    }

    [Fact]
    public void Create_SetsCreatedAtToCurrentUtc()
    {
        var before = DateTime.UtcNow;
        var preference = DormPreference.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        var after = DateTime.UtcNow;
        preference.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }
}
