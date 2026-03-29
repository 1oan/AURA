using Aura.Application.Common.Interfaces;
using Aura.Application.Faculties.Common;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Faculties.Queries.GetFacultyById;

public record GetFacultyByIdQuery(Guid Id) : IRequest<FacultyDto>;

public class GetFacultyByIdQueryHandler : IRequestHandler<GetFacultyByIdQuery, FacultyDto>
{
    private readonly IFacultyRepository _facultyRepository;

    public GetFacultyByIdQueryHandler(IFacultyRepository facultyRepository)
    {
        _facultyRepository = facultyRepository;
    }

    public async Task<FacultyDto> Handle(GetFacultyByIdQuery request, CancellationToken cancellationToken)
    {
        var faculty = await _facultyRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Faculty with id '{request.Id}' was not found.");

        return new FacultyDto(faculty.Id, faculty.Name, faculty.Abbreviation);
    }
}
