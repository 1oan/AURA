using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class DormPreferenceRepository(AuraDbContext context) : IDormPreferenceRepository
{
    public async Task<List<DormPreference>> GetByUserAndPeriodAsync(Guid userId, Guid allocationPeriodId, CancellationToken cancellationToken = default)
    {
        return await context.DormPreferences
            .Include(dp => dp.Dormitory!)
                .ThenInclude(d => d.Campus)
            .Where(dp => dp.UserId == userId && dp.AllocationPeriodId == allocationPeriodId)
            .OrderBy(dp => dp.Rank)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByPeriodAndFacultyAsync(Guid allocationPeriodId, Guid facultyId, CancellationToken cancellationToken = default)
    {
        return await context.DormPreferences
            .Where(dp => dp.AllocationPeriodId == allocationPeriodId)
            .Where(dp => dp.User!.FacultyId == facultyId)
            .Select(dp => dp.UserId)
            .Distinct()
            .CountAsync(cancellationToken);
    }

    public async Task<List<DormPreference>> GetByPeriodAndUsersAsync(
        Guid allocationPeriodId, List<Guid> userIds, CancellationToken cancellationToken = default)
    {
        return await context.DormPreferences
            .Include(dp => dp.Dormitory)
            .Where(dp => dp.AllocationPeriodId == allocationPeriodId && userIds.Contains(dp.UserId))
            .OrderBy(dp => dp.Rank)
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<DormPreference> preferences, CancellationToken cancellationToken = default)
    {
        await context.DormPreferences.AddRangeAsync(preferences, cancellationToken);
    }

    public async Task DeleteByUserAndPeriodAsync(Guid userId, Guid allocationPeriodId, CancellationToken cancellationToken = default)
    {
        await context.DormPreferences
            .Where(dp => dp.UserId == userId && dp.AllocationPeriodId == allocationPeriodId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
