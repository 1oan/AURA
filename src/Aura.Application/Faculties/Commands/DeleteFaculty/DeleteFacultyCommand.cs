using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Faculties.Commands.DeleteFaculty;

public record DeleteFacultyCommand(Guid Id) : IRequest<Unit>;

public class DeleteFacultyCommandHandler : IRequestHandler<DeleteFacultyCommand, Unit>
{
    private readonly IFacultyRepository _facultyRepository;

    public DeleteFacultyCommandHandler(IFacultyRepository facultyRepository)
    {
        _facultyRepository = facultyRepository;
    }

    public async Task<Unit> Handle(DeleteFacultyCommand request, CancellationToken cancellationToken)
    {
        var faculty = await _facultyRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException($"Faculty with id '{request.Id}' was not found.");

        if (await _facultyRepository.HasAllocationsAsync(request.Id, cancellationToken))
            throw new DomainException("Cannot delete faculty that has room allocations.");

        _facultyRepository.Remove(faculty);
        await _facultyRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
