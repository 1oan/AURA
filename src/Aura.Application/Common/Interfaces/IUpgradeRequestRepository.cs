using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IUpgradeRequestRepository
{
    Task<List<UpgradeRequest>> GetActiveTargetingDormAsync(
        Guid dormitoryId, Guid allocationPeriodId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
