using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Commands.CreateRoommateGroup;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Commands;

public class CreateRoommateGroupCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IRoommateGroupRepository _groups = Substitute.For<IRoommateGroupRepository>();
    private readonly IDormAllocationRepository _allocations = Substitute.For<IDormAllocationRepository>();
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();

    private CreateRoommateGroupCommandHandler CreateHandler() =>
        new(_currentUser, _groups, _allocations, _periods);

    private AllocationPeriod CreateAllocatingPeriod()
    {
        var period = AllocationPeriod.Create("test", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(1), 7);
        period.SetPrivateProperty("Id", _periodId);
        period.Activate();
        period.StartAllocating();
        return period;
    }

    [Fact]
    public async Task Handle_HappyPath_CreatesGroupWithLeader()
    {
        var period = CreateAllocatingPeriod();
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.Accept();

        _currentUser.GetCurrentUserId().Returns(_userId);
        _periods.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<AllocationPeriod> { period });
        _allocations.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>()).Returns(allocation);
        _groups.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>()).Returns((RoommateGroup?)null);

        var result = await CreateHandler().Handle(
            new CreateRoommateGroupCommand(RoomSizePreference.ThreeBed), CancellationToken.None);

        await _groups.Received(1).AddAsync(
            Arg.Is<RoommateGroup>(g =>
                g.LeaderUserId == _userId
                && g.DormitoryId == _dormId
                && g.AllocationPeriodId == _periodId
                && g.RoomSizePreference == RoomSizePreference.ThreeBed),
            Arg.Any<CancellationToken>());
        await _groups.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_NoAcceptedAllocation_Throws()
    {
        var period = CreateAllocatingPeriod();

        _currentUser.GetCurrentUserId().Returns(_userId);
        _periods.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<AllocationPeriod> { period });
        _allocations.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>()).Returns((DormAllocation?)null);

        var act = async () => await CreateHandler().Handle(
            new CreateRoommateGroupCommand(RoomSizePreference.TwoBed), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*accepted*");
    }

    [Fact]
    public async Task Handle_AlreadyInGroup_Throws()
    {
        var period = CreateAllocatingPeriod();
        var allocation = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        allocation.Accept();
        var existing = RoommateGroup.Create(_periodId, _dormId, _userId, RoomSizePreference.TwoBed);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _periods.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<AllocationPeriod> { period });
        _allocations.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>()).Returns(allocation);
        _groups.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>()).Returns(existing);

        var act = async () => await CreateHandler().Handle(
            new CreateRoommateGroupCommand(RoomSizePreference.TwoBed), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*already*");
    }
}
