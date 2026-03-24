using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class CampusRepository(AuraDbContext context) : ICampusRepository
{
    public async Task<Campus?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Campuses.FindAsync([id], cancellationToken);

    public async Task<Campus?> FindByIdWithDormitoriesAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Campuses
            .Include(c => c.Dormitories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<List<Campus>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Campuses.OrderBy(c => c.Name).ToListAsync(cancellationToken);

    public async Task<bool> HasDormitoriesAsync(Guid campusId, CancellationToken cancellationToken = default)
        => await context.Dormitories.AnyAsync(d => d.CampusId == campusId, cancellationToken);

    public async Task AddAsync(Campus campus, CancellationToken cancellationToken = default)
        => await context.Campuses.AddAsync(campus, cancellationToken);

    public void Remove(Campus campus) => context.Campuses.Remove(campus);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
