using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class DormAllocationRepository(AuraDbContext context) : IDormAllocationRepository
{
    public async Task<DormAllocation?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.DormAllocations
            .Include(a => a.Dormitory)
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<DormAllocation?> FindActiveByUserAndPeriodAsync(
        Guid userId, Guid allocationPeriodId, CancellationToken cancellationToken = default)
    {
        return await context.DormAllocations
            .Include(a => a.Dormitory!)
                .ThenInclude(d => d.Campus)
            .Where(a => a.UserId == userId && a.AllocationPeriodId == allocationPeriodId)
            .Where(a => a.Status == AllocationStatus.Pending || a.Status == AllocationStatus.Accepted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<DormAllocation>> GetByPeriodAsync(
        Guid allocationPeriodId, CancellationToken cancellationToken = default)
    {
        return await context.DormAllocations
            .Include(a => a.User)
            .Include(a => a.Dormitory!)
                .ThenInclude(d => d.Campus)
            .Where(a => a.AllocationPeriodId == allocationPeriodId)
            .OrderByDescending(a => a.AllocatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<DormAllocation>> GetByPeriodAndFacultyAsync(
        Guid allocationPeriodId, Guid facultyId, CancellationToken cancellationToken = default)
    {
        return await context.DormAllocations
            .Include(a => a.User)
            .Include(a => a.Dormitory!)
                .ThenInclude(d => d.Campus)
            .Where(a => a.AllocationPeriodId == allocationPeriodId)
            .Where(a => a.User!.FacultyId == facultyId)
            .OrderByDescending(a => a.AllocatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetMaxRoundAsync(
        Guid allocationPeriodId, CancellationToken cancellationToken = default)
    {
        return await context.DormAllocations
            .Where(a => a.AllocationPeriodId == allocationPeriodId)
            .Select(a => (int?)a.Round)
            .MaxAsync(cancellationToken) ?? 0;
    }

    public async Task<bool> HasTerminalForUserAndPeriodAsync(
        Guid userId, Guid allocationPeriodId, CancellationToken cancellationToken = default)
    {
        return await context.DormAllocations
            .Where(a => a.UserId == userId && a.AllocationPeriodId == allocationPeriodId)
            .Where(a => a.Status == AllocationStatus.Declined || a.Status == AllocationStatus.Expired)
            .AnyAsync(cancellationToken);
    }

    public async Task<int> GetEffectiveCapacityAsync(
        Guid dormitoryId, Guid facultyId, Gender gender, Guid allocationPeriodId, CancellationToken cancellationToken = default)
    {
        return await context.FacultyRoomAllocations
            .Where(fra => fra.AllocationPeriodId == allocationPeriodId)
            .Where(fra => fra.FacultyId == facultyId)
            .Where(fra => fra.Room!.DormitoryId == dormitoryId)
            .Where(fra => fra.Room!.Gender == gender)
            .SumAsync(fra => fra.Room!.Capacity, cancellationToken);
    }

    public async Task<int> GetUsedCapacityAsync(
        Guid dormitoryId, Guid facultyId, Gender gender, Guid allocationPeriodId, CancellationToken cancellationToken = default)
    {
        return await context.DormAllocations
            .Where(a => a.AllocationPeriodId == allocationPeriodId)
            .Where(a => a.DormitoryId == dormitoryId)
            .Where(a => a.User!.FacultyId == facultyId)
            .Where(a => a.User!.Gender == gender)
            .Where(a => a.Status == AllocationStatus.Pending || a.Status == AllocationStatus.Accepted)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetAvailableCapacityAsync(
        Guid dormitoryId, Guid facultyId, Gender gender, Guid allocationPeriodId, CancellationToken cancellationToken = default)
    {
        var effective = await GetEffectiveCapacityAsync(dormitoryId, facultyId, gender, allocationPeriodId, cancellationToken);
        var used = await GetUsedCapacityAsync(dormitoryId, facultyId, gender, allocationPeriodId, cancellationToken);
        return effective - used;
    }

    public async Task<List<DormAllocation>> GetExpirablePendingAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        // Pull all Pending records into memory to filter by AllocatedAt + ResponseWindowDays <= now.
        // EF/Npgsql cannot translate DateTime.AddDays with a per-row int column in SQL,
        // but the Pending set is small (bounded by active allocation rounds).
        var allPending = await context.DormAllocations
            .Include(a => a.AllocationPeriod)
            .Where(a => a.Status == AllocationStatus.Pending)
            .ToListAsync(cancellationToken);

        return allPending
            .Where(a => a.AllocationPeriod != null
                && a.AllocatedAt.AddDays(a.AllocationPeriod.ResponseWindowDays) <= now)
            .ToList();
    }

    public async Task<List<DormAllocation>> GetPendingFromPriorRoundsAsync(Guid allocationPeriodId, int currentRound, CancellationToken cancellationToken = default)
    {
        return await context.DormAllocations
            .Where(a => a.AllocationPeriodId == allocationPeriodId)
            .Where(a => a.Status == AllocationStatus.Pending)
            .Where(a => a.Round < currentRound)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(DormAllocation allocation, CancellationToken cancellationToken = default)
    {
        await context.DormAllocations.AddAsync(allocation, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
