using Aura.Application.Campuses.Common;
using Aura.Application.Common.Interfaces;
using Aura.Application.Dormitories.Common;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Campuses.Queries.GetCampusById;

public record GetCampusByIdQuery(Guid Id) : IRequest<CampusDetailDto>;

public class GetCampusByIdQueryHandler : IRequestHandler<GetCampusByIdQuery, CampusDetailDto>
{
    private readonly ICampusRepository _campusRepository;

    public GetCampusByIdQueryHandler(ICampusRepository campusRepository)
    {
        _campusRepository = campusRepository;
    }

    public async Task<CampusDetailDto> Handle(GetCampusByIdQuery request, CancellationToken cancellationToken)
    {
        var campus = await _campusRepository.FindByIdWithDormitoriesAsync(request.Id, cancellationToken)
            ?? throw new DomainException($"Campus with id '{request.Id}' was not found.");

        var dormitories = campus.Dormitories
            .Select(d => new DormitoryDto(d.Id, d.Name, campus.Id))
            .ToList();

        return new CampusDetailDto(campus.Id, campus.Name, campus.Address, dormitories);
    }
}
