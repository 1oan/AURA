using Aura.Application.Campuses.Common;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using MediatR;

namespace Aura.Application.Campuses.Commands.CreateCampus;

public record CreateCampusCommand(string Name, string? Address) : IRequest<CampusDto>;

public class CreateCampusCommandHandler : IRequestHandler<CreateCampusCommand, CampusDto>
{
    private readonly ICampusRepository _campusRepository;

    public CreateCampusCommandHandler(ICampusRepository campusRepository)
    {
        _campusRepository = campusRepository;
    }

    public async Task<CampusDto> Handle(CreateCampusCommand request, CancellationToken cancellationToken)
    {
        var campus = Campus.Create(request.Name, request.Address);

        await _campusRepository.AddAsync(campus, cancellationToken);
        await _campusRepository.SaveChangesAsync(cancellationToken);

        return new CampusDto(campus.Id, campus.Name, campus.Address);
    }
}
