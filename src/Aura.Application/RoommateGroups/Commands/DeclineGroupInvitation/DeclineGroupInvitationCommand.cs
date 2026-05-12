using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.RoommateGroups.Commands.DeclineGroupInvitation;

public record DeclineGroupInvitationCommand(Guid InvitationId) : IRequest;

public class DeclineGroupInvitationCommandHandler(
    ICurrentUserService currentUser,
    IGroupInvitationRepository invitationRepository) : IRequestHandler<DeclineGroupInvitationCommand>
{
    public async Task Handle(DeclineGroupInvitationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        var invitation = await invitationRepository.FindByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new DomainException("Invitation not found.");
        if (invitation.InviteeUserId != userId)
            throw new DomainException("Only the invitee can decline this invitation.");

        invitation.Decline();
        await invitationRepository.SaveChangesAsync(cancellationToken);
    }
}
