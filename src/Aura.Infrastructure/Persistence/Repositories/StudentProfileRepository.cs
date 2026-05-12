using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class StudentProfileRepository(AuraDbContext context) : IStudentProfileRepository
{
    public async Task<StudentProfile?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task AddAsync(StudentProfile profile, CancellationToken cancellationToken = default)
        => await context.StudentProfiles.AddAsync(profile, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
