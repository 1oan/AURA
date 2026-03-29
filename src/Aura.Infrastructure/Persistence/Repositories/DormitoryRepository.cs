using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class DormitoryRepository(AuraDbContext context) : IDormitoryRepository
{
    public async Task<Dormitory?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Dormitories.FindAsync([id], cancellationToken);

    public async Task<Dormitory?> FindByIdWithRoomsAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Dormitories
            .Include(d => d.Rooms)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public async Task<List<Dormitory>> GetByCampusIdAsync(Guid campusId, CancellationToken cancellationToken = default)
        => await context.Dormitories
            .Where(d => d.CampusId == campusId)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

    public async Task<bool> HasRoomsAsync(Guid dormitoryId, CancellationToken cancellationToken = default)
        => await context.Rooms.AnyAsync(r => r.DormitoryId == dormitoryId, cancellationToken);

    public async Task AddAsync(Dormitory dormitory, CancellationToken cancellationToken = default)
        => await context.Dormitories.AddAsync(dormitory, cancellationToken);

    public void Remove(Dormitory dormitory) => context.Dormitories.Remove(dormitory);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
