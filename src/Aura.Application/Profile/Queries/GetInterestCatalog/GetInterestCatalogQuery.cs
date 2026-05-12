using Aura.Application.Common.Interfaces;
using Aura.Application.Profile.Common;
using MediatR;

namespace Aura.Application.Profile.Queries.GetInterestCatalog;

public record GetInterestCatalogQuery : IRequest<List<InterestCategoryDto>>;

public class GetInterestCatalogQueryHandler(IInterestRepository interestRepository)
    : IRequestHandler<GetInterestCatalogQuery, List<InterestCategoryDto>>
{
    public async Task<List<InterestCategoryDto>> Handle(GetInterestCatalogQuery request, CancellationToken cancellationToken)
    {
        var interests = await interestRepository.GetActiveAsync(cancellationToken);

        return interests
            .GroupBy(i => i.Category)
            .Select(g => new InterestCategoryDto(
                g.Key,
                g.OrderBy(i => i.DisplayOrder)
                    .Select(i => new InterestItemDto(i.Slug, i.Label, i.DisplayOrder))
                    .ToList()))
            .ToList();
    }
}
