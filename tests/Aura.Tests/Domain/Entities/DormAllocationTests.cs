using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class DormAllocationTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidArguments_ReturnsPendingAllocation()
    {
        var before = DateTime.UtcNow;
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, round: 1);
        var after = DateTime.UtcNow;

        allocation.Id.Should().NotBe(Guid.Empty);
        allocation.UserId.Should().Be(_userId);
        allocation.DormitoryId.Should().Be(_dormId);
        allocation.AllocationPeriodId.Should().Be(_periodId);
        allocation.Round.Should().Be(1);
        allocation.Status.Should().Be(AllocationStatus.Pending);
        allocation.AllocatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        allocation.RespondedAt.Should().BeNull();
        allocation.ReminderSentAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        var act = () => DormAllocation.Create(Guid.Empty, _dormId, _periodId, 1);
        act.Should().Throw<DomainException>().WithMessage("User ID is required.");
    }

    [Fact]
    public void Create_WithEmptyDormitoryId_Throws()
    {
        var act = () => DormAllocation.Create(_userId, Guid.Empty, _periodId, 1);
        act.Should().Throw<DomainException>().WithMessage("Dormitory ID is required.");
    }

    [Fact]
    public void Create_WithEmptyPeriodId_Throws()
    {
        var act = () => DormAllocation.Create(_userId, _dormId, Guid.Empty, 1);
        act.Should().Throw<DomainException>().WithMessage("Allocation period ID is required.");
    }

    [Fact]
    public void Create_WithRoundLessThan1_Throws()
    {
        var act = () => DormAllocation.Create(_userId, _dormId, _periodId, 0);
        act.Should().Throw<DomainException>().WithMessage("Round must be at least 1.");
    }

    [Fact]
    public void Accept_FromPending_TransitionsToAccepted()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        var before = DateTime.UtcNow;
        allocation.Accept();
        allocation.Status.Should().Be(AllocationStatus.Accepted);
        allocation.RespondedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Accept_FromNonPending_Throws()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.Decline();
        var act = () => allocation.Accept();
        act.Should().Throw<DomainException>().WithMessage("*status Declined*");
    }

    [Fact]
    public void Decline_FromPending_TransitionsToDeclined()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.Decline();
        allocation.Status.Should().Be(AllocationStatus.Declined);
        allocation.RespondedAt.Should().NotBeNull();
    }

    [Fact]
    public void Decline_FromAccepted_Throws()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.Accept();
        var act = () => allocation.Decline();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Expire_FromPending_TransitionsToExpired()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.Expire();
        allocation.Status.Should().Be(AllocationStatus.Expired);
        allocation.RespondedAt.Should().NotBeNull();
    }

    [Fact]
    public void Expire_FromAccepted_Throws()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.Accept();
        var act = () => allocation.Expire();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Replace_FromPending_TransitionsToReplaced()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.Replace();
        allocation.Status.Should().Be(AllocationStatus.Replaced);
        allocation.RespondedAt.Should().NotBeNull();
    }

    [Fact]
    public void Replace_FromAccepted_TransitionsToReplaced()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.Accept();
        allocation.Replace();
        allocation.Status.Should().Be(AllocationStatus.Replaced);
    }

    [Fact]
    public void Replace_FromDeclined_Throws()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.Decline();
        var act = () => allocation.Replace();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MarkReminderSent_WhenPending_SetsTimestamp()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        var before = DateTime.UtcNow;
        allocation.MarkReminderSent();
        var after = DateTime.UtcNow;

        allocation.ReminderSentAt.Should().NotBeNull();
        allocation.ReminderSentAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        allocation.Status.Should().Be(AllocationStatus.Pending);
    }

    [Fact]
    public void MarkReminderSent_WhenAccepted_ThrowsDomainException()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.Accept();
        var act = () => allocation.MarkReminderSent();
        act.Should().Throw<DomainException>().WithMessage("Reminder is only relevant for Pending allocations.");
    }

    [Fact]
    public void MarkReminderSent_WhenDeclined_ThrowsDomainException()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.Decline();
        var act = () => allocation.MarkReminderSent();
        act.Should().Throw<DomainException>().WithMessage("Reminder is only relevant for Pending allocations.");
    }

    [Fact]
    public void MarkReminderSent_WhenExpired_ThrowsDomainException()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.Expire();
        var act = () => allocation.MarkReminderSent();
        act.Should().Throw<DomainException>().WithMessage("Reminder is only relevant for Pending allocations.");
    }

    [Fact]
    public void MarkReminderSent_WhenReplaced_ThrowsDomainException()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.Replace();
        var act = () => allocation.MarkReminderSent();
        act.Should().Throw<DomainException>().WithMessage("Reminder is only relevant for Pending allocations.");
    }

    [Fact]
    public void MarkReminderSent_WhenAlreadySent_ThrowsDomainException()
    {
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.MarkReminderSent();

        var act = () => allocation.MarkReminderSent();

        act.Should().Throw<DomainException>()
            .WithMessage("Reminder has already been sent for this allocation.");
    }
}