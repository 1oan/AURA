using Aura.Application.Campuses.Common;
using Aura.Application.Common.Interfaces;
using MediatR;

namespace Aura.Application.Campuses.Queries.GetCampuses;

public record GetCampusesQuery : IRequest<List<CampusDto>>;

public class GetCampusesQueryHandler : IRequestHandler<GetCampusesQuery, List<CampusDto>>
{
    private readonly ICampusRepository _campusRepository;

    public GetCampusesQueryHandler(ICampusRepository campusRepository)
    {
        _campusRepository = campusRepository;
    }

    public async Task<List<CampusDto>> Handle(GetCampusesQuery request, CancellationToken cancellationToken)
    {
        var campuses = await _campusRepository.GetAllAsync(cancellationToken);

        return campuses
            .Select(c => new CampusDto(c.Id, c.Name, c.Address))
            .ToList();
    }
}
