using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class UpgradeRequestRepository(AuraDbContext context) : IUpgradeRequestRepository
{
    public async Task<UpgradeRequest?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.UpgradeRequests
            .Include(r => r.User)
            .Include(r => r.Targets.OrderBy(t => t.Rank))
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<UpgradeRequest?> FindActiveByUserAndPeriodAsync(
        Guid userId, Guid allocationPeriodId, CancellationToken cancellationToken = default)
    {
        return await context.UpgradeRequests
            .Include(r => r.Targets.OrderBy(t => t.Rank))
            .Where(r => r.IsActive)
            .Where(r => r.UserId == userId)
            .Where(r => r.AllocationPeriodId == allocationPeriodId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<UpgradeRequest>> GetActiveTargetingDormAsync(
        Guid dormitoryId, Guid allocationPeriodId, CancellationToken cancellationToken = default)
    {
        return await context.UpgradeRequests
            .Include(r => r.User)
            .Include(r => r.Targets)
            .Where(r => r.IsActive)
            .Where(r => r.AllocationPeriodId == allocationPeriodId)
            .Where(r => r.Targets.Any(t => t.DormitoryId == dormitoryId))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UpgradeRequest>> GetActiveForPeriodAsync(
        Guid allocationPeriodId, CancellationToken cancellationToken = default)
    {
        return await context.UpgradeRequests
            .Include(r => r.Targets)
            .Where(r => r.IsActive)
            .Where(r => r.AllocationPeriodId == allocationPeriodId)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UpgradeRequest request, CancellationToken cancellationToken = default)
    {
        await context.UpgradeRequests.AddAsync(request, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
