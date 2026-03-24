using Aura.Application.Common.Interfaces;
using Aura.Application.Dormitories.Common;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Dormitories.Commands.CreateDormitory;

public record CreateDormitoryCommand(string Name, Guid CampusId) : IRequest<DormitoryDto>;

public class CreateDormitoryCommandHandler : IRequestHandler<CreateDormitoryCommand, DormitoryDto>
{
    private readonly IDormitoryRepository _dormitoryRepository;
    private readonly ICampusRepository _campusRepository;

    public CreateDormitoryCommandHandler(IDormitoryRepository dormitoryRepository, ICampusRepository campusRepository)
    {
        _dormitoryRepository = dormitoryRepository;
        _campusRepository = campusRepository;
    }

    public async Task<DormitoryDto> Handle(CreateDormitoryCommand request, CancellationToken cancellationToken)
    {
        var campus = await _campusRepository.FindByIdAsync(request.CampusId, cancellationToken)
            ?? throw new DomainException($"Campus with id '{request.CampusId}' was not found.");

        var dormitory = Dormitory.Create(request.Name, campus.Id);

        await _dormitoryRepository.AddAsync(dormitory, cancellationToken);
        await _dormitoryRepository.SaveChangesAsync(cancellationToken);

        return new DormitoryDto(dormitory.Id, dormitory.Name, dormitory.CampusId);
    }
}
