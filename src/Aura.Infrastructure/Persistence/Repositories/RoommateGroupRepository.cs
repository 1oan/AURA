using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class RoommateGroupRepository(AuraDbContext context) : IRoommateGroupRepository
{
    public async Task<RoommateGroup?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.RoommateGroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    public async Task<RoommateGroup?> FindActiveByUserAndPeriodAsync(
        Guid userId, Guid allocationPeriodId, CancellationToken cancellationToken = default)
        => await context.RoommateGroups
            .Include(g => g.Members)
            .Where(g => g.AllocationPeriodId == allocationPeriodId)
            .Where(g => g.Status == GroupStatus.Forming || g.Status == GroupStatus.Locked)
            .FirstOrDefaultAsync(g => g.Members.Any(m => m.UserId == userId), cancellationToken);

    public async Task<RoommateGroup?> GetActiveGroupForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.RoommateGroups
            .Include(g => g.Members)
            .Where(g => g.Status == GroupStatus.Forming || g.Status == GroupStatus.Locked)
            .FirstOrDefaultAsync(g => g.Members.Any(m => m.UserId == userId), cancellationToken);

    public async Task<List<RoommateGroup>> GetExpiredOverdueAsync(DateTime utcNow, CancellationToken cancellationToken = default)
        => await context.RoommateGroups
            .Include(g => g.Members)
            .Where(g => g.Status == GroupStatus.Forming && g.ExpiresAt < utcNow)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(RoommateGroup group, CancellationToken cancellationToken = default)
        => await context.RoommateGroups.AddAsync(group, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
