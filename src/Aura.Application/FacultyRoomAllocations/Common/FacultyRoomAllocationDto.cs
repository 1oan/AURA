namespace Aura.Application.FacultyRoomAllocations.Common;

public record FacultyRoomAllocationDto(Guid Id, Guid FacultyId, Guid RoomId, Guid AllocationPeriodId);
