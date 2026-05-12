using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Commands.LockGroup;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Events;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using MediatR;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Commands;

public class LockGroupCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IRoommateGroupRepository _groups = Substitute.For<IRoommateGroupRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private readonly Guid _leaderId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _groupId = Guid.NewGuid();

    private LockGroupCommandHandler CreateHandler() =>
        new(_currentUser, _groups, _publisher);

    private RoommateGroup CreateGroup(RoomSizePreference pref, params Guid[] additionalMembers)
    {
        var group = RoommateGroup.Create(_periodId, _dormId, _leaderId, pref);
        group.SetPrivateProperty("Id", _groupId);
        foreach (var member in additionalMembers)
            group.AddMember(member);
        return group;
    }

    [Fact]
    public async Task Handle_HappyPath_LocksAndPublishesEvent()
    {
        var group = CreateGroup(RoomSizePreference.TwoBed, Guid.NewGuid());
        _currentUser.GetCurrentUserId().Returns(_leaderId);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        await CreateHandler().Handle(
            new LockGroupCommand(_groupId), CancellationToken.None);

        group.Status.Should().Be(GroupStatus.Locked);
        await _groups.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<GroupLockedEvent>(e => e.GroupId == _groupId && e.LeaderUserId == _leaderId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotLeader_Throws()
    {
        var group = CreateGroup(RoomSizePreference.TwoBed, Guid.NewGuid());
        _currentUser.GetCurrentUserId().Returns(Guid.NewGuid());
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        var act = async () => await CreateHandler().Handle(
            new LockGroupCommand(_groupId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*leader*");
    }

    [Fact]
    public async Task Handle_BelowCapacity_Throws()
    {
        var group = CreateGroup(RoomSizePreference.ThreeBed, Guid.NewGuid());
        _currentUser.GetCurrentUserId().Returns(_leaderId);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        var act = async () => await CreateHandler().Handle(
            new LockGroupCommand(_groupId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*capacity*");
    }
}
