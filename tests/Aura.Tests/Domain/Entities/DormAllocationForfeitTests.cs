using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class DormAllocationForfeitTests
{
    [Fact]
    public void Forfeit_FromAccepted_TransitionsToForfeited()
    {
        var allocation = DormAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        allocation.Accept();

        allocation.Forfeit();

        allocation.Status.Should().Be(AllocationStatus.Forfeited);
    }

    [Fact]
    public void Forfeit_FromAccepted_StampsRespondedAt()
    {
        var allocation = DormAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        allocation.Accept();
        var before = DateTime.UtcNow;

        allocation.Forfeit();

        allocation.RespondedAt.Should().NotBeNull();
        allocation.RespondedAt.Should().BeOnOrAfter(before);
    }

    [Theory]
    [InlineData(AllocationStatus.Pending)]
    [InlineData(AllocationStatus.Declined)]
    [InlineData(AllocationStatus.Expired)]
    [InlineData(AllocationStatus.Replaced)]
    [InlineData(AllocationStatus.Forfeited)]
    public void Forfeit_FromNonAccepted_Throws(AllocationStatus initial)
    {
        var allocation = DormAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1);
        TransitionTo(allocation, initial);

        var act = () => allocation.Forfeit();

        act.Should().Throw<DomainException>()
            .WithMessage($"*Cannot forfeit allocation in status {initial}*");
    }

    private static void TransitionTo(DormAllocation allocation, AllocationStatus target)
    {
        switch (target)
        {
            case AllocationStatus.Pending: break;
            case AllocationStatus.Accepted: allocation.Accept(); break;
            case AllocationStatus.Declined: allocation.Decline(); break;
            case AllocationStatus.Expired: allocation.Expire(); break;
            case AllocationStatus.Replaced: allocation.Replace(); break;
            case AllocationStatus.Forfeited:
                allocation.Accept();
                allocation.Forfeit();
                break;
        }
    }
}
