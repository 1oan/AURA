using Aura.Application.Common.Interfaces;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.RoommateGroups.Commands.ChangeRoomSizePreference;

public record ChangeRoomSizePreferenceCommand(Guid GroupId, RoomSizePreference NewPreference) : IRequest;

public class ChangeRoomSizePreferenceCommandHandler(
    ICurrentUserService currentUser,
    IRoommateGroupRepository groupRepository) : IRequestHandler<ChangeRoomSizePreferenceCommand>
{
    public async Task Handle(ChangeRoomSizePreferenceCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        var group = await groupRepository.FindByIdAsync(request.GroupId, cancellationToken)
            ?? throw new DomainException("Group not found.");
        if (group.LeaderUserId != userId)
            throw new DomainException("Only the leader can change room size preference.");

        group.ChangeRoomSizePreference(request.NewPreference);
        await groupRepository.SaveChangesAsync(cancellationToken);
    }
}
