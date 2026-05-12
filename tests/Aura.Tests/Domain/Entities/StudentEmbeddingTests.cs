using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;
using Pgvector;

namespace Aura.Tests.Domain.Entities;

public class StudentEmbeddingTests
{
    [Fact]
    public void Create_ValidUserId_ReturnsEmptyEmbedding()
    {
        var userId = Guid.NewGuid();
        var embedding = StudentEmbedding.Create(userId);

        embedding.UserId.Should().Be(userId);
        embedding.Embedding.Should().BeNull();
        embedding.LastEmbeddedAt.Should().BeNull();
        embedding.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_EmptyUserId_Throws()
    {
        var act = () => StudentEmbedding.Create(Guid.Empty);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void UpdateEmbedding_ValidVector_StoresAndStampsTimestamp()
    {
        var embedding = StudentEmbedding.Create(Guid.NewGuid());
        var vector = new Vector(new float[384]);

        embedding.UpdateEmbedding(vector);

        embedding.Embedding.Should().NotBeNull();
        embedding.LastEmbeddedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void UpdateEmbedding_WrongDimension_Throws()
    {
        var embedding = StudentEmbedding.Create(Guid.NewGuid());
        var wrongVector = new Vector(new float[128]);

        var act = () => embedding.UpdateEmbedding(wrongVector);

        act.Should().Throw<DomainException>().WithMessage("*384*");
    }
}
