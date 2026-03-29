using Aura.Application.Rooms.Common;

namespace Aura.Application.Dormitories.Common;

public record DormitoryDetailDto(Guid Id, string Name, Guid CampusId, List<RoomDto> Rooms);
