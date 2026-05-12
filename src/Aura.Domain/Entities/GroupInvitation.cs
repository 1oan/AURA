using Aura.Domain.Enums;
using Aura.Domain.Exceptions;

namespace Aura.Domain.Entities;

public class GroupInvitation
{
    public Guid Id { get; private set; }
    public Guid GroupId { get; private set; }
    public Guid InviterUserId { get; private set; }
    public Guid InviteeUserId { get; private set; }
    public InvitationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }

    private GroupInvitation() { }

    public static GroupInvitation Create(Guid groupId, Guid inviterUserId, Guid inviteeUserId)
    {
        if (groupId == Guid.Empty) throw new DomainException("Group id is required.");
        if (inviterUserId == Guid.Empty) throw new DomainException("Inviter id is required.");
        if (inviteeUserId == Guid.Empty) throw new DomainException("Invitee id is required.");
        if (inviterUserId == inviteeUserId) throw new DomainException("Cannot invite self.");

        return new GroupInvitation
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            InviterUserId = inviterUserId,
            InviteeUserId = inviteeUserId,
            Status = InvitationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Accept() => Transition(InvitationStatus.Accepted);
    public void Decline() => Transition(InvitationStatus.Declined);
    public void Cancel() => Transition(InvitationStatus.Cancelled);
    public void Expire() => Transition(InvitationStatus.Expired);

    private void Transition(InvitationStatus to)
    {
        if (Status != InvitationStatus.Pending)
            throw new DomainException($"Invitation is {Status}; only Pending invitations can transition.");
        Status = to;
        RespondedAt = DateTime.UtcNow;
    }
}
