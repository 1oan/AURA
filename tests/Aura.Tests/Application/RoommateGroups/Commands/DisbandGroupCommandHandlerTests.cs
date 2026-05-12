using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Commands.DisbandGroup;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Commands;

public class DisbandGroupCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IRoommateGroupRepository _groups = Substitute.For<IRoommateGroupRepository>();
    private readonly IGroupInvitationRepository _invitations = Substitute.For<IGroupInvitationRepository>();

    private readonly Guid _leaderId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _groupId = Guid.NewGuid();

    private DisbandGroupCommandHandler CreateHandler() =>
        new(_currentUser, _groups, _invitations);

    private RoommateGroup CreateGroup()
    {
        var group = RoommateGroup.Create(_periodId, _dormId, _leaderId, RoomSizePreference.ThreeBed);
        group.SetPrivateProperty("Id", _groupId);
        return group;
    }

    [Fact]
    public async Task Handle_HappyPath_CancelsPendingInvitationsAndDisbands()
    {
        var group = CreateGroup();
        var invitation1 = GroupInvitation.Create(_groupId, _leaderId, Guid.NewGuid());
        var invitation2 = GroupInvitation.Create(_groupId, _leaderId, Guid.NewGuid());

        _currentUser.GetCurrentUserId().Returns(_leaderId);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);
        _invitations.GetPendingForGroupAsync(_groupId, Arg.Any<CancellationToken>())
            .Returns(new List<GroupInvitation> { invitation1, invitation2 });

        await CreateHandler().Handle(
            new DisbandGroupCommand(_groupId), CancellationToken.None);

        invitation1.Status.Should().Be(InvitationStatus.Cancelled);
        invitation2.Status.Should().Be(InvitationStatus.Cancelled);
        group.Status.Should().Be(GroupStatus.Disbanded);
        await _invitations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _groups.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotLeader_Throws()
    {
        var group = CreateGroup();
        _currentUser.GetCurrentUserId().Returns(Guid.NewGuid());
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        var act = async () => await CreateHandler().Handle(
            new DisbandGroupCommand(_groupId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*leader*");
    }

    [Fact]
    public async Task Handle_GroupNotForming_Throws()
    {
        var group = RoommateGroup.Create(_periodId, _dormId, _leaderId, RoomSizePreference.TwoBed);
        group.SetPrivateProperty("Id", _groupId);
        group.AddMember(Guid.NewGuid());
        group.Lock();

        _currentUser.GetCurrentUserId().Returns(_leaderId);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);
        _invitations.GetPendingForGroupAsync(_groupId, Arg.Any<CancellationToken>())
            .Returns(new List<GroupInvitation>());

        var act = async () => await CreateHandler().Handle(
            new DisbandGroupCommand(_groupId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*disband*");
    }
}
