using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Commands.ChangeRoomSizePreference;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Commands;

public class ChangeRoomSizePreferenceCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IRoommateGroupRepository _groups = Substitute.For<IRoommateGroupRepository>();

    private readonly Guid _leaderId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _groupId = Guid.NewGuid();

    private ChangeRoomSizePreferenceCommandHandler CreateHandler() =>
        new(_currentUser, _groups);

    private RoommateGroup CreateGroup(RoomSizePreference pref, params Guid[] additionalMembers)
    {
        var group = RoommateGroup.Create(_periodId, _dormId, _leaderId, pref);
        group.SetPrivateProperty("Id", _groupId);
        foreach (var member in additionalMembers)
            group.AddMember(member);
        return group;
    }

    [Fact]
    public async Task Handle_HappyPath_ChangesPreferenceAndSaves()
    {
        var group = CreateGroup(RoomSizePreference.ThreeBed);
        _currentUser.GetCurrentUserId().Returns(_leaderId);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        await CreateHandler().Handle(
            new ChangeRoomSizePreferenceCommand(_groupId, RoomSizePreference.TwoBed), CancellationToken.None);

        group.RoomSizePreference.Should().Be(RoomSizePreference.TwoBed);
        await _groups.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotLeader_Throws()
    {
        var group = CreateGroup(RoomSizePreference.ThreeBed);
        _currentUser.GetCurrentUserId().Returns(Guid.NewGuid());
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        var act = async () => await CreateHandler().Handle(
            new ChangeRoomSizePreferenceCommand(_groupId, RoomSizePreference.TwoBed), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*leader*");
    }

    [Fact]
    public async Task Handle_BelowMemberCount_Throws()
    {
        var group = CreateGroup(RoomSizePreference.ThreeBed, Guid.NewGuid(), Guid.NewGuid());
        _currentUser.GetCurrentUserId().Returns(_leaderId);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        var act = async () => await CreateHandler().Handle(
            new ChangeRoomSizePreferenceCommand(_groupId, RoomSizePreference.TwoBed), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*below current member count*");
    }
}
