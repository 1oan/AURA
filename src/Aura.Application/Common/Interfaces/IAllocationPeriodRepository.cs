using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IAllocationPeriodRepository
{
    Task<AllocationPeriod?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<AllocationPeriod>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> AnyActiveAsync(CancellationToken cancellationToken = default);
    Task AddAsync(AllocationPeriod period, CancellationToken cancellationToken = default);
    void Remove(AllocationPeriod period);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
