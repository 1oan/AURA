using Aura.Application.Common.Interfaces;
using Aura.Application.RoomAssignments.Commands.PlaceMeNow;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.RoomAssignments.Commands;

public class PlaceMeNowCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IRoommateGroupRepository _groupRepository = Substitute.For<IRoommateGroupRepository>();
    private readonly IRoomAssignmentService _roomAssignmentService = Substitute.For<IRoomAssignmentService>();

    private readonly Guid _userId = Guid.NewGuid();

    private PlaceMeNowCommandHandler CreateHandler() =>
        new(_currentUser, _groupRepository, _roomAssignmentService);

    [Fact]
    public async Task Handle_NoActiveGroup_CallsPlaceSoloAsync()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _groupRepository.GetActiveGroupForUserAsync(_userId, Arg.Any<CancellationToken>())
            .Returns((RoommateGroup?)null);
        _roomAssignmentService.PlaceSoloAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(RoomAssignment.Create(_userId, Guid.NewGuid(), Guid.NewGuid()));

        var result = await CreateHandler().Handle(new PlaceMeNowCommand(), CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        await _roomAssignmentService.Received(1).PlaceSoloAsync(_userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FormingMemberInGroup_ThrowsDomainException()
    {
        var leaderId = Guid.NewGuid();
        var group = RoommateGroup.Create(Guid.NewGuid(), Guid.NewGuid(), leaderId, RoomSizePreference.TwoBed);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _groupRepository.GetActiveGroupForUserAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(group);

        var act = async () => await CreateHandler().Handle(new PlaceMeNowCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*forming group*");
    }

    [Fact]
    public async Task Handle_FormingLeaderInGroup_ThrowsDomainException()
    {
        var group = RoommateGroup.Create(Guid.NewGuid(), Guid.NewGuid(), _userId, RoomSizePreference.TwoBed);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _groupRepository.GetActiveGroupForUserAsync(_userId, Arg.Any<CancellationToken>())
            .Returns(group);

        var act = async () => await CreateHandler().Handle(new PlaceMeNowCommand(), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Lock or disband*");
    }
}
