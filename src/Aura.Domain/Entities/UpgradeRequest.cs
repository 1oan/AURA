using Aura.Domain.Exceptions;

namespace Aura.Domain.Entities;

public class UpgradeRequest
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid AllocationPeriodId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; }
    public ICollection<UpgradeRequestTarget> Targets { get; private set; } = new List<UpgradeRequestTarget>();

    public User? User { get; private set; }
    public AllocationPeriod? AllocationPeriod { get; private set; }

    private UpgradeRequest() { }

    public static UpgradeRequest Create(Guid userId, Guid allocationPeriodId, IEnumerable<Guid> orderedDormitoryIds)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User ID is required.");
        if (allocationPeriodId == Guid.Empty)
            throw new DomainException("Allocation period ID is required.");

        var targets = orderedDormitoryIds.ToList();
        if (targets.Count == 0)
            throw new DomainException("At least one upgrade target is required.");
        if (targets.Distinct().Count() != targets.Count)
            throw new DomainException("Upgrade targets must be unique.");

        var request = new UpgradeRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AllocationPeriodId = allocationPeriodId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
        };

        for (int i = 0; i < targets.Count; i++)
        {
            request.Targets.Add(UpgradeRequestTarget.Create(request.Id, targets[i], rank: i + 1));
        }

        return request;
    }

    public void Fulfill() => IsActive = false;
    public void Cancel() => IsActive = false;
}