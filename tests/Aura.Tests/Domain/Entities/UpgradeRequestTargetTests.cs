using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class UpgradeRequestTargetTests
{
    [Fact]
    public void Create_WithValidArgs_Succeeds()
    {
        var requestId = Guid.NewGuid();
        var dormId = Guid.NewGuid();
        var target = UpgradeRequestTarget.Create(requestId, dormId, rank: 1);
        target.Id.Should().NotBe(Guid.Empty);
        target.UpgradeRequestId.Should().Be(requestId);
        target.DormitoryId.Should().Be(dormId);
        target.Rank.Should().Be(1);
    }

    [Fact]
    public void Create_WithEmptyRequestId_Throws()
    {
        var act = () => UpgradeRequestTarget.Create(Guid.Empty, Guid.NewGuid(), 1);
        act.Should().Throw<DomainException>().WithMessage("Upgrade request ID is required.");
    }

    [Fact]
    public void Create_WithEmptyDormitoryId_Throws()
    {
        var act = () => UpgradeRequestTarget.Create(Guid.NewGuid(), Guid.Empty, 1);
        act.Should().Throw<DomainException>().WithMessage("Dormitory ID is required.");
    }

    [Fact]
    public void Create_WithRankBelow1_Throws()
    {
        var act = () => UpgradeRequestTarget.Create(Guid.NewGuid(), Guid.NewGuid(), 0);
        act.Should().Throw<DomainException>().WithMessage("Target rank must be at least 1.");
    }
}