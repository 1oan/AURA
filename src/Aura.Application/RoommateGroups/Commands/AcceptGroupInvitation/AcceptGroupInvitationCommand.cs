using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.RoommateGroups.Commands.AcceptGroupInvitation;

public record AcceptGroupInvitationCommand(Guid InvitationId) : IRequest;

public class AcceptGroupInvitationCommandHandler(
    ICurrentUserService currentUser,
    IGroupInvitationRepository invitationRepository,
    IRoommateGroupRepository groupRepository,
    IPublisher publisher) : IRequestHandler<AcceptGroupInvitationCommand>
{
    public async Task Handle(AcceptGroupInvitationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        var invitation = await invitationRepository.FindByIdAsync(request.InvitationId, cancellationToken)
            ?? throw new DomainException("Invitation not found.");
        if (invitation.InviteeUserId != userId)
            throw new DomainException("Only the invitee can accept this invitation.");

        var group = await groupRepository.FindByIdAsync(invitation.GroupId, cancellationToken)
            ?? throw new DomainException("Group not found.");
        if (group.Status != GroupStatus.Forming)
            throw new DomainException("Group is no longer accepting members.");

        var existing = await groupRepository.FindActiveByUserAndPeriodAsync(userId, group.AllocationPeriodId, cancellationToken);
        if (existing is not null)
            throw new DomainException("You are already in another active group.");

        invitation.Accept();
        await invitationRepository.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new GroupInvitationAcceptedEvent(invitation.Id, group.Id, userId),
            cancellationToken);
    }
}
