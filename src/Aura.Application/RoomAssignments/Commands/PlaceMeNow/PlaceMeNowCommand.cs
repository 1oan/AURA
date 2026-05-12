using Aura.Application.Common.Interfaces;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.RoomAssignments.Commands.PlaceMeNow;

public record PlaceMeNowCommand : IRequest<Unit>;

public class PlaceMeNowCommandHandler(
    ICurrentUserService currentUser,
    IRoommateGroupRepository groupRepository,
    IRoomAssignmentService roomAssignmentService) : IRequestHandler<PlaceMeNowCommand, Unit>
{
    public async Task<Unit> Handle(PlaceMeNowCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();

        var group = await groupRepository.GetActiveGroupForUserAsync(userId, cancellationToken);

        if (group is not null && group.Status == GroupStatus.Forming)
        {
            if (userId == group.LeaderUserId)
                throw new DomainException("Lock or disband your group before requesting solo placement.");

            throw new DomainException("You are in a forming group. Ask your leader to lock it before requesting placement.");
        }

        await roomAssignmentService.PlaceSoloAsync(userId, cancellationToken);

        return Unit.Value;
    }
}
