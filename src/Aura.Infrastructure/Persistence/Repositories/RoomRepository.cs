using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class RoomRepository(AuraDbContext context) : IRoomRepository
{
    public async Task<Room?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Rooms.FindAsync([id], cancellationToken);

    public async Task<List<Room>> GetByDormitoryIdAsync(Guid dormitoryId, CancellationToken cancellationToken = default)
        => await context.Rooms
            .Where(r => r.DormitoryId == dormitoryId)
            .OrderBy(r => r.Floor)
            .ThenBy(r => r.Number)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsByNumberInDormitoryAsync(Guid dormitoryId, string number, CancellationToken cancellationToken = default)
        => await context.Rooms.AnyAsync(r => r.DormitoryId == dormitoryId && r.Number == number, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<Room> rooms, CancellationToken cancellationToken = default)
        => await context.Rooms.AddRangeAsync(rooms, cancellationToken);

    public void Remove(Room room) => context.Rooms.Remove(room);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
