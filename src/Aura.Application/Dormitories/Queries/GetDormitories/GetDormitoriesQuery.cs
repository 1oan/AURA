using Aura.Application.Common.Interfaces;
using Aura.Application.Dormitories.Common;
using MediatR;

namespace Aura.Application.Dormitories.Queries.GetDormitories;

public record GetDormitoriesQuery(Guid CampusId) : IRequest<List<DormitoryDto>>;

public class GetDormitoriesQueryHandler : IRequestHandler<GetDormitoriesQuery, List<DormitoryDto>>
{
    private readonly IDormitoryRepository _dormitoryRepository;

    public GetDormitoriesQueryHandler(IDormitoryRepository dormitoryRepository)
    {
        _dormitoryRepository = dormitoryRepository;
    }

    public async Task<List<DormitoryDto>> Handle(GetDormitoriesQuery request, CancellationToken cancellationToken)
    {
        var dormitories = await _dormitoryRepository.GetByCampusIdAsync(request.CampusId, cancellationToken);

        return dormitories
            .Select(d => new DormitoryDto(d.Id, d.Name, d.CampusId))
            .ToList();
    }
}
