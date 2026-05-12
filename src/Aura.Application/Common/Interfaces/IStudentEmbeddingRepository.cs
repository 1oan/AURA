using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IStudentEmbeddingRepository
{
    Task<StudentEmbedding?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(StudentEmbedding embedding, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
