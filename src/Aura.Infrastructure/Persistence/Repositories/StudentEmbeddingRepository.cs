using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class StudentEmbeddingRepository(AuraDbContext context) : IStudentEmbeddingRepository
{
    public async Task<StudentEmbedding?> FindByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.StudentEmbeddings.FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

    public async Task AddAsync(StudentEmbedding embedding, CancellationToken cancellationToken = default)
        => await context.StudentEmbeddings.AddAsync(embedding, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
