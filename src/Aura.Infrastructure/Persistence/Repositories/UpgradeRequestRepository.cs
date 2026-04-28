using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class UpgradeRequestRepository(AuraDbContext context) : IUpgradeRequestRepository
{
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

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}
