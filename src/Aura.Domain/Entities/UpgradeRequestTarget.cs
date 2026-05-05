using Aura.Domain.Exceptions;

namespace Aura.Domain.Entities;

public class UpgradeRequestTarget
{
    public Guid Id { get; private set; }
    public Guid UpgradeRequestId { get; private set; }
    public Guid DormitoryId { get; private set; }
    public int Rank { get; private set; }

    public Dormitory? Dormitory { get; private set; }
    public UpgradeRequest? UpgradeRequest { get; private set; }

    private UpgradeRequestTarget() { }

    internal static UpgradeRequestTarget Create(Guid upgradeRequestId, Guid dormitoryId, int rank)
    {
        if (upgradeRequestId == Guid.Empty)
            throw new DomainException("Upgrade request ID is required.");
        if (dormitoryId == Guid.Empty)
            throw new DomainException("Dormitory ID is required.");
        if (rank < 1)
            throw new DomainException("Target rank must be at least 1.");

        return new UpgradeRequestTarget
        {
            Id = Guid.NewGuid(),
            UpgradeRequestId = upgradeRequestId,
            DormitoryId = dormitoryId,
            Rank = rank,
        };
    }
}