using Aura.Application.Common.Interfaces;
using Aura.Application.Rooms.Common;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Rooms.Queries.GetRoomById;

public record GetRoomByIdQuery(Guid Id) : IRequest<RoomDto>;

public class GetRoomByIdQueryHandler : IRequestHandler<GetRoomByIdQuery, RoomDto>
{
    private readonly IRoomRepository _roomRepository;

    public GetRoomByIdQueryHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<RoomDto> Handle(GetRoomByIdQuery request, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException($"Room with id '{request.Id}' was not found.");

        return new RoomDto(room.Id, room.Number, room.DormitoryId, room.Floor, room.Capacity, room.Gender.ToString());
    }
}
