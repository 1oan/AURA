using Aura.Domain.Exceptions;
using Pgvector;

namespace Aura.Domain.Entities;

public class StudentEmbedding
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Vector? Embedding { get; private set; }
    public DateTime? LastEmbeddedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private StudentEmbedding() { }

    public static StudentEmbedding Create(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User id is required.");

        return new StudentEmbedding
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void UpdateEmbedding(Vector vector)
    {
        ArgumentNullException.ThrowIfNull(vector);
        if (vector.ToArray().Length != 384)
            throw new DomainException("Embedding must have exactly 384 dimensions.");

        Embedding = vector;
        LastEmbeddedAt = DateTime.UtcNow;
    }
}
