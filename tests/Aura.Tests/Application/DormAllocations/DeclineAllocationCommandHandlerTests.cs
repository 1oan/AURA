using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Commands.DeclineAllocation;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using FluentAssertions;
using MediatR;
using NSubstitute;

namespace Aura.Tests.Application.DormAllocations;

public class DeclineAllocationCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IDormAllocationRepository _allocations = Substitute.For<IDormAllocationRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _allocationId = Guid.NewGuid();

    private DeclineAllocationCommandHandler CreateHandler() => new(_currentUser, _allocations, _publisher);

    private DormAllocation PendingAllocationOwnedByUser()
    {
        return DormAllocation.Create(_userId, Guid.NewGuid(), Guid.NewGuid(), 1);
    }

    [Fact]
    public async Task Handle_AllocationNotFound_ThrowsNotFoundException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _allocations.FindByIdAsync(_allocationId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);

        var act = async () => await CreateHandler().Handle(
            new DeclineAllocationCommand(_allocationId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*not found*");
        await _allocations.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<AllocationDeclinedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotOwner_ThrowsDomainException()
    {
        var someoneElseId = Guid.NewGuid();
        _currentUser.GetCurrentUserId().Returns(someoneElseId);
        var allocation = PendingAllocationOwnedByUser();
        _allocations.FindByIdAsync(_allocationId, Arg.Any<CancellationToken>()).Returns(allocation);

        var act = async () => await CreateHandler().Handle(
            new DeclineAllocationCommand(_allocationId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*own allocation*");
        await _allocations.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<AllocationDeclinedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StatusNotPending_ThrowsDomainException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        var allocation = PendingAllocationOwnedByUser();
        allocation.Decline();
        _allocations.FindByIdAsync(_allocationId, Arg.Any<CancellationToken>()).Returns(allocation);

        var act = async () => await CreateHandler().Handle(
            new DeclineAllocationCommand(_allocationId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Cannot decline*");
        await _allocations.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<AllocationDeclinedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_TransitionsStatusAndSaves()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        var allocation = PendingAllocationOwnedByUser();
        _allocations.FindByIdAsync(_allocationId, Arg.Any<CancellationToken>()).Returns(allocation);

        await CreateHandler().Handle(new DeclineAllocationCommand(_allocationId), CancellationToken.None);

        allocation.Status.Should().Be(AllocationStatus.Declined);
        await _allocations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_PublishesAllocationDeclinedEvent()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        var allocation = PendingAllocationOwnedByUser();
        _allocations.FindByIdAsync(_allocationId, Arg.Any<CancellationToken>()).Returns(allocation);

        await CreateHandler().Handle(new DeclineAllocationCommand(_allocationId), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<AllocationDeclinedEvent>(e =>
                e.AllocationId == allocation.Id
                && e.UserId == allocation.UserId
                && e.DormitoryId == allocation.DormitoryId
                && e.AllocationPeriodId == allocation.AllocationPeriodId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_SaveBeforePublish()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        var allocation = PendingAllocationOwnedByUser();
        _allocations.FindByIdAsync(_allocationId, Arg.Any<CancellationToken>()).Returns(allocation);

        var callOrder = new List<string>();
        _allocations.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("save"));
        _publisher.Publish(Arg.Any<AllocationDeclinedEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("publish"));

        await CreateHandler().Handle(new DeclineAllocationCommand(_allocationId), CancellationToken.None);

        callOrder.Should().Equal("save", "publish");
    }
}
