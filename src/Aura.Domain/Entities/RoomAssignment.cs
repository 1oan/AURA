using Aura.Domain.Exceptions;

namespace Aura.Domain.Entities;

public class RoomAssignment
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid AllocationPeriodId { get; private set; }
    public Guid? RoommateGroupId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    public User? User { get; private set; }
    public Room? Room { get; private set; }
    public AllocationPeriod? AllocationPeriod { get; private set; }
    public RoommateGroup? RoommateGroup { get; private set; }

    private RoomAssignment() { }

    public static RoomAssignment Create(
        Guid userId,
        Guid roomId,
        Guid allocationPeriodId,
        Guid? roommateGroupId = null)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User ID is required.");
        if (roomId == Guid.Empty)
            throw new DomainException("Room ID is required.");
        if (allocationPeriodId == Guid.Empty)
            throw new DomainException("Allocation period ID is required.");

        return new RoomAssignment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoomId = roomId,
            AllocationPeriodId = allocationPeriodId,
            RoommateGroupId = roommateGroupId,
            AssignedAt = DateTime.UtcNow,
        };
    }
}
