using Aura.Domain.Entities;
using Aura.Domain.Events;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class AllocationPeriodClosedEventTests
{
    [Fact]
    public void Close_FromAllocating_RaisesAllocationPeriodClosedEvent()
    {
        var period = AllocationPeriod.Create(
            "2026 Fall",
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(-20),
            7);
        period.Activate();
        period.StartAllocating();

        period.Close();

        period.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<AllocationPeriodClosedEvent>()
            .Which.AllocationPeriodId.Should().Be(period.Id);
    }

    [Fact]
    public void Close_FromNonAllocating_DoesNotRaiseEvent()
    {
        var period = AllocationPeriod.Create(
            "2026 Fall",
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(-20),
            7);

        var act = () => period.Close();

        act.Should().Throw<Exception>();
        period.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllRaisedEvents()
    {
        var period = AllocationPeriod.Create(
            "2026 Fall",
            DateTime.UtcNow.AddDays(-30),
            DateTime.UtcNow.AddDays(30),
            DateTime.UtcNow.AddDays(-20),
            7);
        period.Activate();
        period.StartAllocating();
        period.Close();

        period.ClearDomainEvents();

        period.DomainEvents.Should().BeEmpty();
    }
}
