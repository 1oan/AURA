using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.RoommateGroups.Commands.DisbandGroup;

public record DisbandGroupCommand(Guid GroupId) : IRequest;

public class DisbandGroupCommandHandler(
    ICurrentUserService currentUser,
    IRoommateGroupRepository groupRepository,
    IGroupInvitationRepository invitationRepository) : IRequestHandler<DisbandGroupCommand>
{
    public async Task Handle(DisbandGroupCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        var group = await groupRepository.FindByIdAsync(request.GroupId, cancellationToken)
            ?? throw new DomainException("Group not found.");
        if (group.LeaderUserId != userId)
            throw new DomainException("Only the leader can disband the group.");

        var pending = await invitationRepository.GetPendingForGroupAsync(group.Id, cancellationToken);
        foreach (var invitation in pending)
            invitation.Cancel();

        group.Disband();

        await invitationRepository.SaveChangesAsync(cancellationToken);
        await groupRepository.SaveChangesAsync(cancellationToken);
    }
}
