namespace Aura.Application.Rooms.Common;

public record RoomDto(Guid Id, string Number, Guid DormitoryId, int Floor, int Capacity, string Gender);
