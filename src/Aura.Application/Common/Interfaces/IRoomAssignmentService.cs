using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IRoomAssignmentService
{
    Task<RoomAssignment> PlaceSoloAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<RoomAssignment>> PlaceGroupAsync(Guid groupId, CancellationToken cancellationToken);
    Task<List<Guid>> ForfeitNonCommittedAsync(Guid allocationPeriodId, CancellationToken cancellationToken);
}
