using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IFacultyRepository
{
    Task<Faculty?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<Faculty>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> HasAllocationsAsync(Guid facultyId, CancellationToken cancellationToken = default);
    Task AddAsync(Faculty faculty, CancellationToken cancellationToken = default);
    void Remove(Faculty faculty);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
