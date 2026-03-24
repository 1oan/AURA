using Aura.Application.Common.Interfaces;
using Aura.Application.Faculties.Common;
using MediatR;

namespace Aura.Application.Faculties.Queries.GetFaculties;

public record GetFacultiesQuery : IRequest<List<FacultyDto>>;

public class GetFacultiesQueryHandler : IRequestHandler<GetFacultiesQuery, List<FacultyDto>>
{
    private readonly IFacultyRepository _facultyRepository;

    public GetFacultiesQueryHandler(IFacultyRepository facultyRepository)
    {
        _facultyRepository = facultyRepository;
    }

    public async Task<List<FacultyDto>> Handle(GetFacultiesQuery request, CancellationToken cancellationToken)
    {
        var faculties = await _facultyRepository.GetAllAsync(cancellationToken);

        return faculties
            .Select(f => new FacultyDto(f.Id, f.Name, f.Abbreviation))
            .ToList();
    }
}
