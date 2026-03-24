using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Campuses.Commands.DeleteCampus;

public record DeleteCampusCommand(Guid Id) : IRequest<Unit>;

public class DeleteCampusCommandHandler : IRequestHandler<DeleteCampusCommand, Unit>
{
    private readonly ICampusRepository _campusRepository;

    public DeleteCampusCommandHandler(ICampusRepository campusRepository)
    {
        _campusRepository = campusRepository;
    }

    public async Task<Unit> Handle(DeleteCampusCommand request, CancellationToken cancellationToken)
    {
        var campus = await _campusRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException($"Campus with id '{request.Id}' was not found.");

        if (await _campusRepository.HasDormitoriesAsync(request.Id, cancellationToken))
            throw new DomainException("Cannot delete campus that has dormitories.");

        _campusRepository.Remove(campus);
        await _campusRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
