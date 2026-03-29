using Aura.Domain.Exceptions;

namespace Aura.Domain.Entities;

public class FacultyRoomAllocation
{
    public Guid Id { get; private set; }
    public Guid FacultyId { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid AllocationPeriodId { get; private set; }
    public Faculty Faculty { get; private set; } = null!;
    public Room Room { get; private set; } = null!;
    public AllocationPeriod AllocationPeriod { get; private set; } = null!;

    private FacultyRoomAllocation() { }

    public static FacultyRoomAllocation Create(Guid facultyId, Guid roomId, Guid allocationPeriodId)
    {
        if (facultyId == Guid.Empty)
            throw new DomainException("Faculty ID is required.");
        if (roomId == Guid.Empty)
            throw new DomainException("Room ID is required.");
        if (allocationPeriodId == Guid.Empty)
            throw new DomainException("Allocation period ID is required.");

        return new FacultyRoomAllocation
        {
            Id = Guid.NewGuid(),
            FacultyId = facultyId,
            RoomId = roomId,
            AllocationPeriodId = allocationPeriodId
        };
    }
}
