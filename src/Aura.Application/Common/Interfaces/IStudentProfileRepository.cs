using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IStudentProfileRepository
{
    Task<StudentProfile?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(StudentProfile profile, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
