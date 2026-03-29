using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IRoomRepository
{
    Task<Room?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Room>> GetByDormitoryIdAsync(Guid dormitoryId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNumberInDormitoryAsync(Guid dormitoryId, string number, CancellationToken cancellationToken = default);
    Task AddAsync(Room room, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Room> rooms, CancellationToken cancellationToken = default);
    void Remove(Room room);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
