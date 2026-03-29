using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Campuses.Commands.UpdateCampus;

public record UpdateCampusCommand(Guid Id, string Name, string? Address) : IRequest<Unit>;

public class UpdateCampusCommandHandler : IRequestHandler<UpdateCampusCommand, Unit>
{
    private readonly ICampusRepository _campusRepository;

    public UpdateCampusCommandHandler(ICampusRepository campusRepository)
    {
        _campusRepository = campusRepository;
    }

    public async Task<Unit> Handle(UpdateCampusCommand request, CancellationToken cancellationToken)
    {
        var campus = await _campusRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Campus with id '{request.Id}' was not found.");

        campus.Update(request.Name, request.Address);

        await _campusRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
