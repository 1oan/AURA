using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface ICampusRepository
{
    Task<Campus?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Campus?> FindByIdWithDormitoriesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Campus>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> HasDormitoriesAsync(Guid campusId, CancellationToken cancellationToken = default);
    Task AddAsync(Campus campus, CancellationToken cancellationToken = default);
    void Remove(Campus campus);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
