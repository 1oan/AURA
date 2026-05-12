using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Commands.DeclineGroupInvitation;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Commands;

public class DeclineGroupInvitationCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IGroupInvitationRepository _invitations = Substitute.For<IGroupInvitationRepository>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _invitationId = Guid.NewGuid();
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _leaderId = Guid.NewGuid();

    private DeclineGroupInvitationCommandHandler CreateHandler() =>
        new(_currentUser, _invitations);

    private GroupInvitation CreateInvitation(Guid? inviteeId = null)
    {
        var invitee = inviteeId ?? _userId;
        var invitation = GroupInvitation.Create(_groupId, _leaderId, invitee);
        invitation.SetPrivateProperty("Id", _invitationId);
        return invitation;
    }

    [Fact]
    public async Task Handle_HappyPath_DeclinesAndSaves()
    {
        var invitation = CreateInvitation();
        _currentUser.GetCurrentUserId().Returns(_userId);
        _invitations.FindByIdAsync(_invitationId, Arg.Any<CancellationToken>()).Returns(invitation);

        await CreateHandler().Handle(
            new DeclineGroupInvitationCommand(_invitationId), CancellationToken.None);

        invitation.Status.Should().Be(InvitationStatus.Declined);
        await _invitations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotInvitee_Throws()
    {
        _currentUser.GetCurrentUserId().Returns(Guid.NewGuid());
        _invitations.FindByIdAsync(_invitationId, Arg.Any<CancellationToken>()).Returns(CreateInvitation());

        var act = async () => await CreateHandler().Handle(
            new DeclineGroupInvitationCommand(_invitationId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*invitee*");
    }
}
