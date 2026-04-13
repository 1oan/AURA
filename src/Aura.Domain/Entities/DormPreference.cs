using Aura.Domain.Exceptions;

namespace Aura.Domain.Entities;

public class DormPreference
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AllocationPeriodId { get; private set; }
    public Guid DormitoryId { get; private set; }
    public int Rank { get; private set; }

    public User? User { get; private set; }
    public AllocationPeriod? AllocationPeriod { get; private set; }
    public Dormitory? Dormitory { get; private set; }

    private DormPreference() { }

    public static DormPreference Create(Guid userId, Guid allocationPeriodId, Guid dormitoryId, int rank)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User ID is required.");
        if (allocationPeriodId == Guid.Empty)
            throw new DomainException("Allocation period ID is required.");
        if (dormitoryId == Guid.Empty)
            throw new DomainException("Dormitory ID is required.");
        if (rank < 1)
            throw new DomainException("Rank must be at least 1.");

        return new DormPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AllocationPeriodId = allocationPeriodId,
            DormitoryId = dormitoryId,
            Rank = rank
        };
    }
}
