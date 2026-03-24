using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class FacultyRepository(AuraDbContext context) : IFacultyRepository
{
    public async Task<Faculty?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.Faculties.FindAsync([id], cancellationToken);

    public async Task<List<Faculty>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Faculties.OrderBy(f => f.Name).ToListAsync(cancellationToken);

    public async Task<bool> HasAllocationsAsync(Guid facultyId, CancellationToken cancellationToken = default)
        => await context.FacultyRoomAllocations.AnyAsync(a => a.FacultyId == facultyId, cancellationToken);

    public async Task AddAsync(Faculty faculty, CancellationToken cancellationToken = default)
        => await context.Faculties.AddAsync(faculty, cancellationToken);

    public void Remove(Faculty faculty) => context.Faculties.Remove(faculty);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
