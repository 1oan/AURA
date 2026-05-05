using Aura.Domain.Enums;
using Aura.Domain.Exceptions;

namespace Aura.Domain.Entities;

public class DormAllocation
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid DormitoryId { get; private set; }
    public Guid AllocationPeriodId { get; private set; }
    public int Round { get; private set; }
    public AllocationStatus Status { get; private set; }
    public DateTime AllocatedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }

    public User? User { get; private set; }
    public Dormitory? Dormitory { get; private set; }
    public AllocationPeriod? AllocationPeriod { get; private set; }

    private DormAllocation() { }

    public static DormAllocation Create(Guid userId, Guid dormitoryId, Guid allocationPeriodId, int round)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User ID is required.");
        if (dormitoryId == Guid.Empty)
            throw new DomainException("Dormitory ID is required.");
        if (allocationPeriodId == Guid.Empty)
            throw new DomainException("Allocation period ID is required.");
        if (round < 1)
            throw new DomainException("Round must be at least 1.");

        return new DormAllocation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DormitoryId = dormitoryId,
            AllocationPeriodId = allocationPeriodId,
            Round = round,
            Status = AllocationStatus.Pending,
            AllocatedAt = DateTime.UtcNow,
            RespondedAt = null,
        };
    }

    public void Accept()
    {
        if (Status != AllocationStatus.Pending)
            throw new DomainException($"Cannot accept allocation in status {Status}.");
        Status = AllocationStatus.Accepted;
        RespondedAt = DateTime.UtcNow;
    }

    public void Decline()
    {
        if (Status != AllocationStatus.Pending)
            throw new DomainException($"Cannot decline allocation in status {Status}.");
        Status = AllocationStatus.Declined;
        RespondedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        if (Status != AllocationStatus.Pending)
            throw new DomainException($"Cannot expire allocation in status {Status}.");
        Status = AllocationStatus.Expired;
        RespondedAt = DateTime.UtcNow;
    }

    public void Replace()
    {
        if (Status != AllocationStatus.Pending && Status != AllocationStatus.Accepted)
            throw new DomainException($"Cannot replace allocation in status {Status}.");
        Status = AllocationStatus.Replaced;
        RespondedAt = DateTime.UtcNow;
    }
}