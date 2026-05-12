using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aura.Application.RoomAssignments.Events;

public class UpgradeRoomReleaseHandler(
    IRoomAssignmentRepository roomAssignmentRepository,
    ILogger<UpgradeRoomReleaseHandler> logger) : INotificationHandler<AllocationReplacedEvent>
{
    public async Task Handle(AllocationReplacedEvent notification, CancellationToken cancellationToken)
    {
        var assignment = await roomAssignmentRepository.FindByUserAndPeriodAsync(
            notification.UserId, notification.AllocationPeriodId, cancellationToken);
        if (assignment is null) return;

        roomAssignmentRepository.Remove(assignment);
        await roomAssignmentRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Released stale room assignment {AssignmentId} for user {UserId} after upgrade in period {PeriodId}.",
            assignment.Id, notification.UserId, notification.AllocationPeriodId);
    }
}
