using Aura.Application.Common.Interfaces;
using Aura.Application.Profile.Queries.GetInterestCatalog;
using Aura.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.Profile.Queries;

public class GetInterestCatalogQueryHandlerTests
{
    private readonly IInterestRepository _interests = Substitute.For<IInterestRepository>();

    private GetInterestCatalogQueryHandler Create() => new(_interests);

    [Fact]
    public async Task Handle_ReturnsActiveGroupedByCategory()
    {
        _interests.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<Interest>
        {
            Interest.Create(Guid.NewGuid(), "football", "Football", "sports", 1),
            Interest.Create(Guid.NewGuid(), "gym", "Gym", "sports", 2),
            Interest.Create(Guid.NewGuid(), "movies", "Movies", "entertainment", 1),
        });

        var result = await Create().Handle(new GetInterestCatalogQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Single(c => c.Category == "sports").Items.Should().HaveCount(2);
        result.Single(c => c.Category == "entertainment").Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_EmptyCatalog_ReturnsEmptyList()
    {
        _interests.GetActiveAsync(Arg.Any<CancellationToken>()).Returns(new List<Interest>());

        var result = await Create().Handle(new GetInterestCatalogQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
