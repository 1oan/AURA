using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IDormitoryRepository
{
    Task<Dormitory?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Dormitory?> FindByIdWithRoomsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Dormitory>> GetByCampusIdAsync(Guid campusId, CancellationToken cancellationToken = default);
    Task<bool> HasRoomsAsync(Guid dormitoryId, CancellationToken cancellationToken = default);
    Task AddAsync(Dormitory dormitory, CancellationToken cancellationToken = default);
    void Remove(Dormitory dormitory);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
