using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class AllocationPeriodRepository(AuraDbContext context) : IAllocationPeriodRepository
{
    public async Task<AllocationPeriod?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.AllocationPeriods.FindAsync([id], cancellationToken);

    public async Task<List<AllocationPeriod>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.AllocationPeriods
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(cancellationToken);

    public async Task<bool> AnyActiveAsync(CancellationToken cancellationToken = default)
        => await context.AllocationPeriods.AnyAsync(
            p => p.Status == AllocationPeriodStatus.Open || p.Status == AllocationPeriodStatus.Allocating,
            cancellationToken);

    public async Task AddAsync(AllocationPeriod period, CancellationToken cancellationToken = default)
        => await context.AllocationPeriods.AddAsync(period, cancellationToken);

    public void Remove(AllocationPeriod period) => context.AllocationPeriods.Remove(period);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
