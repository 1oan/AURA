using Aura.Application.Common.Interfaces;
using Aura.Application.Faculties.Common;
using Aura.Domain.Entities;
using MediatR;

namespace Aura.Application.Faculties.Commands.CreateFaculty;

public record CreateFacultyCommand(string Name, string Abbreviation) : IRequest<FacultyDto>;

public class CreateFacultyCommandHandler : IRequestHandler<CreateFacultyCommand, FacultyDto>
{
    private readonly IFacultyRepository _facultyRepository;

    public CreateFacultyCommandHandler(IFacultyRepository facultyRepository)
    {
        _facultyRepository = facultyRepository;
    }

    public async Task<FacultyDto> Handle(CreateFacultyCommand request, CancellationToken cancellationToken)
    {
        var faculty = Faculty.Create(request.Name, request.Abbreviation);

        await _facultyRepository.AddAsync(faculty, cancellationToken);
        await _facultyRepository.SaveChangesAsync(cancellationToken);

        return new FacultyDto(faculty.Id, faculty.Name, faculty.Abbreviation);
    }
}
