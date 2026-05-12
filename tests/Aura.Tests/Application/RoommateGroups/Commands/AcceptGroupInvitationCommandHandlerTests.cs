using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Commands.AcceptGroupInvitation;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using MediatR;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Commands;

public class AcceptGroupInvitationCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IGroupInvitationRepository _invitations = Substitute.For<IGroupInvitationRepository>();
    private readonly IRoommateGroupRepository _groups = Substitute.For<IRoommateGroupRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _invitationId = Guid.NewGuid();
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _leaderId = Guid.NewGuid();

    private AcceptGroupInvitationCommandHandler CreateHandler() =>
        new(_currentUser, _invitations, _groups, _publisher);

    private GroupInvitation CreateInvitation(Guid? inviteeId = null)
    {
        var invitee = inviteeId ?? _userId;
        var invitation = GroupInvitation.Create(_groupId, _leaderId, invitee);
        invitation.SetPrivateProperty("Id", _invitationId);
        return invitation;
    }

    private RoommateGroup CreateFormingGroup()
    {
        var group = RoommateGroup.Create(_periodId, _dormId, _leaderId, RoomSizePreference.ThreeBed);
        group.SetPrivateProperty("Id", _groupId);
        return group;
    }

    [Fact]
    public async Task Handle_HappyPath_AcceptsAndPublishesEvent()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _invitations.FindByIdAsync(_invitationId, Arg.Any<CancellationToken>()).Returns(CreateInvitation());
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(CreateFormingGroup());
        _groups.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns((RoommateGroup?)null);

        await CreateHandler().Handle(
            new AcceptGroupInvitationCommand(_invitationId), CancellationToken.None);

        await _invitations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<GroupInvitationAcceptedEvent>(e => e.GroupId == _groupId && e.InviteeUserId == _userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotInvitee_Throws()
    {
        _currentUser.GetCurrentUserId().Returns(Guid.NewGuid());
        _invitations.FindByIdAsync(_invitationId, Arg.Any<CancellationToken>()).Returns(CreateInvitation());

        var act = async () => await CreateHandler().Handle(
            new AcceptGroupInvitationCommand(_invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*invitee*");
    }

    [Fact]
    public async Task Handle_GroupNotForming_Throws()
    {
        var group = CreateFormingGroup();
        group.AddMember(Guid.NewGuid());
        group.AddMember(Guid.NewGuid());
        group.Lock();

        _currentUser.GetCurrentUserId().Returns(_userId);
        _invitations.FindByIdAsync(_invitationId, Arg.Any<CancellationToken>()).Returns(CreateInvitation());
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        var act = async () => await CreateHandler().Handle(
            new AcceptGroupInvitationCommand(_invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*no longer*");
    }

    [Fact]
    public async Task Handle_AlreadyInAnotherGroup_Throws()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _invitations.FindByIdAsync(_invitationId, Arg.Any<CancellationToken>()).Returns(CreateInvitation());
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(CreateFormingGroup());
        _groups.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(RoommateGroup.Create(_periodId, _dormId, _userId, RoomSizePreference.TwoBed));

        var act = async () => await CreateHandler().Handle(
            new AcceptGroupInvitationCommand(_invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*already in another*");
    }
}
