using Aura.Application.Common.Interfaces;
using Aura.Application.Dormitories.Common;
using Aura.Application.Rooms.Common;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Dormitories.Queries.GetDormitoryById;

public record GetDormitoryByIdQuery(Guid Id) : IRequest<DormitoryDetailDto>;

public class GetDormitoryByIdQueryHandler : IRequestHandler<GetDormitoryByIdQuery, DormitoryDetailDto>
{
    private readonly IDormitoryRepository _dormitoryRepository;

    public GetDormitoryByIdQueryHandler(IDormitoryRepository dormitoryRepository)
    {
        _dormitoryRepository = dormitoryRepository;
    }

    public async Task<DormitoryDetailDto> Handle(GetDormitoryByIdQuery request, CancellationToken cancellationToken)
    {
        var dormitory = await _dormitoryRepository.FindByIdWithRoomsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Dormitory with id '{request.Id}' was not found.");

        var rooms = dormitory.Rooms
            .Select(r => new RoomDto(r.Id, r.Number, r.DormitoryId, r.Floor, r.Capacity, r.Gender.ToString()))
            .ToList();

        return new DormitoryDetailDto(dormitory.Id, dormitory.Name, dormitory.CampusId, rooms);
    }
}
