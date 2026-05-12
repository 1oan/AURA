using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IRoomAssignmentRepository
{
    Task<RoomAssignment?> FindByUserAndPeriodAsync(Guid userId, Guid allocationPeriodId, CancellationToken cancellationToken = default);
    Task<int> CountOccupantsAsync(Guid roomId, Guid allocationPeriodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoomAssignment>> ListByRoomAndPeriodAsync(Guid roomId, Guid allocationPeriodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, int>> GetOccupancyForDormitoryAsync(Guid dormitoryId, Guid allocationPeriodId, CancellationToken cancellationToken = default);
    Task AddAsync(RoomAssignment assignment, CancellationToken cancellationToken = default);
    void Remove(RoomAssignment assignment);
    Task<IReadOnlyList<RoomAssignment>> ListRoommatesAsync(Guid userId, Guid roomId, Guid allocationPeriodId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
