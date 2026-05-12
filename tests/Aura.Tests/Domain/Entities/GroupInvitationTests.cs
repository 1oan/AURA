using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class GroupInvitationTests
{
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _inviterId = Guid.NewGuid();
    private readonly Guid _inviteeId = Guid.NewGuid();

    [Fact]
    public void Create_ValidArgs_ReturnsPendingInvitation()
    {
        var invitation = GroupInvitation.Create(_groupId, _inviterId, _inviteeId);

        invitation.GroupId.Should().Be(_groupId);
        invitation.InviterUserId.Should().Be(_inviterId);
        invitation.InviteeUserId.Should().Be(_inviteeId);
        invitation.Status.Should().Be(InvitationStatus.Pending);
        invitation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        invitation.RespondedAt.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyGroupId_Throws()
    {
        var act = () => GroupInvitation.Create(Guid.Empty, _inviterId, _inviteeId);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_InviterEqualsInvitee_Throws()
    {
        var sameId = Guid.NewGuid();
        var act = () => GroupInvitation.Create(_groupId, sameId, sameId);
        act.Should().Throw<DomainException>().WithMessage("*self*");
    }

    [Fact]
    public void Accept_FromPending_TransitionsToAccepted()
    {
        var invitation = GroupInvitation.Create(_groupId, _inviterId, _inviteeId);
        invitation.Accept();
        invitation.Status.Should().Be(InvitationStatus.Accepted);
        invitation.RespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Decline_FromPending_TransitionsToDeclined()
    {
        var invitation = GroupInvitation.Create(_groupId, _inviterId, _inviteeId);
        invitation.Decline();
        invitation.Status.Should().Be(InvitationStatus.Declined);
        invitation.RespondedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Cancel_FromPending_TransitionsToCancelled()
    {
        var invitation = GroupInvitation.Create(_groupId, _inviterId, _inviteeId);
        invitation.Cancel();
        invitation.Status.Should().Be(InvitationStatus.Cancelled);
    }

    [Fact]
    public void Expire_FromPending_TransitionsToExpired()
    {
        var invitation = GroupInvitation.Create(_groupId, _inviterId, _inviteeId);
        invitation.Expire();
        invitation.Status.Should().Be(InvitationStatus.Expired);
    }

    [Fact]
    public void Accept_AfterDecline_Throws()
    {
        var invitation = GroupInvitation.Create(_groupId, _inviterId, _inviteeId);
        invitation.Decline();
        var act = () => invitation.Accept();
        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }

    [Fact]
    public void Decline_AfterAccept_Throws()
    {
        var invitation = GroupInvitation.Create(_groupId, _inviterId, _inviteeId);
        invitation.Accept();
        var act = () => invitation.Decline();
        act.Should().Throw<DomainException>().WithMessage("*Pending*");
    }
}
