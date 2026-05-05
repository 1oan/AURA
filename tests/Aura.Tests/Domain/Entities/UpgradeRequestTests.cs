using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class UpgradeRequestTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidArgs_AssignsTargetsInRankOrder()
    {
        var d1 = Guid.NewGuid();
        var d2 = Guid.NewGuid();
        var d3 = Guid.NewGuid();
        var request = UpgradeRequest.Create(_userId, _periodId, [d1, d2, d3]);

        request.Id.Should().NotBe(Guid.Empty);
        request.UserId.Should().Be(_userId);
        request.AllocationPeriodId.Should().Be(_periodId);
        request.IsActive.Should().BeTrue();
        request.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        request.Targets.Should().HaveCount(3);
        request.Targets.Single(t => t.Rank == 1).DormitoryId.Should().Be(d1);
        request.Targets.Single(t => t.Rank == 2).DormitoryId.Should().Be(d2);
        request.Targets.Single(t => t.Rank == 3).DormitoryId.Should().Be(d3);
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        var act = () => UpgradeRequest.Create(Guid.Empty, _periodId, [Guid.NewGuid()]);
        act.Should().Throw<DomainException>().WithMessage("User ID is required.");
    }

    [Fact]
    public void Create_WithEmptyPeriodId_Throws()
    {
        var act = () => UpgradeRequest.Create(_userId, Guid.Empty, [Guid.NewGuid()]);
        act.Should().Throw<DomainException>().WithMessage("Allocation period ID is required.");
    }

    [Fact]
    public void Create_WithEmptyTargets_Throws()
    {
        var act = () => UpgradeRequest.Create(_userId, _periodId, []);
        act.Should().Throw<DomainException>().WithMessage("At least one upgrade target is required.");
    }

    [Fact]
    public void Create_WithDuplicateTargets_Throws()
    {
        var d = Guid.NewGuid();
        var act = () => UpgradeRequest.Create(_userId, _periodId, [d, d]);
        act.Should().Throw<DomainException>().WithMessage("Upgrade targets must be unique.");
    }

    [Fact]
    public void Fulfill_SetsIsActiveFalse()
    {
        var request = UpgradeRequest.Create(_userId, _periodId, [Guid.NewGuid()]);
        request.Fulfill();
        request.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Cancel_SetsIsActiveFalse()
    {
        var request = UpgradeRequest.Create(_userId, _periodId, [Guid.NewGuid()]);
        request.Cancel();
        request.IsActive.Should().BeFalse();
    }
}