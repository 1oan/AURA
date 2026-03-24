using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Faculties.Commands.UpdateFaculty;

public record UpdateFacultyCommand(Guid Id, string Name, string Abbreviation) : IRequest<Unit>;

public class UpdateFacultyCommandHandler : IRequestHandler<UpdateFacultyCommand, Unit>
{
    private readonly IFacultyRepository _facultyRepository;

    public UpdateFacultyCommandHandler(IFacultyRepository facultyRepository)
    {
        _facultyRepository = facultyRepository;
    }

    public async Task<Unit> Handle(UpdateFacultyCommand request, CancellationToken cancellationToken)
    {
        var faculty = await _facultyRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException($"Faculty with id '{request.Id}' was not found.");

        faculty.Update(request.Name, request.Abbreviation);

        await _facultyRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
