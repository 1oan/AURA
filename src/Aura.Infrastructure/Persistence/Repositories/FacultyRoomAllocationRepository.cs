using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class FacultyRoomAllocationRepository(AuraDbContext context) : IFacultyRoomAllocationRepository
{
    public async Task<List<FacultyRoomAllocation>> GetByPeriodAndFacultyAsync(
        Guid? periodId,
        Guid? facultyId,
        CancellationToken cancellationToken = default)
    {
        var query = context.FacultyRoomAllocations
            .Include(a => a.Faculty)
            .Include(a => a.Room)
                .ThenInclude(r => r.Dormitory)
            .Include(a => a.AllocationPeriod)
            .AsQueryable();

        if (periodId.HasValue)
            query = query.Where(a => a.AllocationPeriodId == periodId.Value);

        if (facultyId.HasValue)
            query = query.Where(a => a.FacultyId == facultyId.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<FacultyRoomAllocation>> GetByRoomAndPeriodAsync(
        Guid roomId,
        Guid periodId,
        CancellationToken cancellationToken = default)
        => await context.FacultyRoomAllocations
            .Include(a => a.Faculty)
            .Include(a => a.Room)
                .ThenInclude(r => r.Dormitory)
            .Include(a => a.AllocationPeriod)
            .Where(a => a.RoomId == roomId && a.AllocationPeriodId == periodId)
            .ToListAsync(cancellationToken);

    public async Task AddRangeAsync(IEnumerable<FacultyRoomAllocation> allocations, CancellationToken cancellationToken = default)
        => await context.FacultyRoomAllocations.AddRangeAsync(allocations, cancellationToken);

    public void RemoveRange(IEnumerable<FacultyRoomAllocation> allocations)
        => context.FacultyRoomAllocations.RemoveRange(allocations);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
