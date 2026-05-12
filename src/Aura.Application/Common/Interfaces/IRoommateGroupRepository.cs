using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IRoommateGroupRepository
{
    Task<RoommateGroup?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RoommateGroup?> FindActiveByUserAndPeriodAsync(Guid userId, Guid allocationPeriodId, CancellationToken cancellationToken = default);
    Task<RoommateGroup?> GetActiveGroupForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<RoommateGroup>> GetExpiredOverdueAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task AddAsync(RoommateGroup group, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
