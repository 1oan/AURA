using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class RoomAssignmentRepository(AuraDbContext db) : IRoomAssignmentRepository
{
    public Task<RoomAssignment?> FindByUserAndPeriodAsync(Guid userId, Guid allocationPeriodId, CancellationToken cancellationToken = default)
        => db.RoomAssignments
            .Include(r => r.Room).ThenInclude(r => r!.Dormitory)
            .FirstOrDefaultAsync(r => r.UserId == userId && r.AllocationPeriodId == allocationPeriodId, cancellationToken);

    public Task<int> CountOccupantsAsync(Guid roomId, Guid allocationPeriodId, CancellationToken cancellationToken = default)
        => db.RoomAssignments.CountAsync(r => r.RoomId == roomId && r.AllocationPeriodId == allocationPeriodId, cancellationToken);

    public async Task<IReadOnlyList<RoomAssignment>> ListByRoomAndPeriodAsync(Guid roomId, Guid allocationPeriodId, CancellationToken cancellationToken = default)
        => await db.RoomAssignments
            .Where(r => r.RoomId == roomId && r.AllocationPeriodId == allocationPeriodId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> GetOccupancyForDormitoryAsync(Guid dormitoryId, Guid allocationPeriodId, CancellationToken cancellationToken = default)
    {
        var grouped = await db.RoomAssignments
            .Where(r => r.Room!.DormitoryId == dormitoryId && r.AllocationPeriodId == allocationPeriodId)
            .GroupBy(r => r.RoomId)
            .Select(g => new { RoomId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return grouped.ToDictionary(g => g.RoomId, g => g.Count);
    }

    public Task AddAsync(RoomAssignment assignment, CancellationToken cancellationToken = default)
    {
        db.RoomAssignments.Add(assignment);
        return Task.CompletedTask;
    }

    public void Remove(RoomAssignment assignment)
        => db.RoomAssignments.Remove(assignment);

    public async Task<IReadOnlyList<RoomAssignment>> ListRoommatesAsync(Guid userId, Guid roomId, Guid allocationPeriodId, CancellationToken cancellationToken = default)
        => await db.RoomAssignments
            .Include(r => r.User)
            .Where(r => r.RoomId == roomId && r.AllocationPeriodId == allocationPeriodId && r.UserId != userId)
            .ToListAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await db.SaveChangesAsync(cancellationToken);
}
