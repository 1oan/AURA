using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IUpgradeRequestRepository
{
    Task<UpgradeRequest?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<UpgradeRequest?> FindActiveByUserAndPeriodAsync(
        Guid userId, Guid allocationPeriodId, CancellationToken cancellationToken = default);

    Task<List<UpgradeRequest>> GetActiveTargetingDormAsync(
        Guid dormitoryId, Guid allocationPeriodId, CancellationToken cancellationToken = default);

    Task<List<UpgradeRequest>> GetActiveForPeriodAsync(
        Guid allocationPeriodId, CancellationToken cancellationToken = default);

    Task AddAsync(UpgradeRequest request, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
