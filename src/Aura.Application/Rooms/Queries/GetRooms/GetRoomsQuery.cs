using Aura.Application.Common.Interfaces;
using Aura.Application.Rooms.Common;
using MediatR;

namespace Aura.Application.Rooms.Queries.GetRooms;

public record GetRoomsQuery(Guid DormitoryId) : IRequest<List<RoomDto>>;

public class GetRoomsQueryHandler : IRequestHandler<GetRoomsQuery, List<RoomDto>>
{
    private readonly IRoomRepository _roomRepository;

    public GetRoomsQueryHandler(IRoomRepository roomRepository)
    {
        _roomRepository = roomRepository;
    }

    public async Task<List<RoomDto>> Handle(GetRoomsQuery request, CancellationToken cancellationToken)
    {
        var rooms = await _roomRepository.GetByDormitoryIdAsync(request.DormitoryId, cancellationToken);

        return rooms
            .Select(r => new RoomDto(r.Id, r.Number, r.DormitoryId, r.Floor, r.Capacity, r.Gender.ToString()))
            .ToList();
    }
}
