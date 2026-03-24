using Aura.Domain.Enums;
using Aura.Domain.Exceptions;

namespace Aura.Domain.Entities;

public class AllocationPeriod
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public AllocationPeriodStatus Status { get; private set; }

    private AllocationPeriod() { }

    public static AllocationPeriod Create(string name, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Allocation period name is required.");
        if (name.Length > 200)
            throw new DomainException("Allocation period name must not exceed 200 characters.");
        if (endDate <= startDate)
            throw new DomainException("End date must be after start date.");

        return new AllocationPeriod
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            Status = AllocationPeriodStatus.Draft
        };
    }

    public void Update(string name, DateTime startDate, DateTime endDate)
    {
        if (Status != AllocationPeriodStatus.Draft)
            throw new DomainException("Can only update allocation period while in Draft status.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Allocation period name is required.");
        if (name.Length > 200)
            throw new DomainException("Allocation period name must not exceed 200 characters.");
        if (endDate <= startDate)
            throw new DomainException("End date must be after start date.");

        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
    }

    public void Activate()
    {
        if (Status != AllocationPeriodStatus.Draft)
            throw new DomainException("Can only activate a Draft allocation period.");
        Status = AllocationPeriodStatus.Open;
    }

    public void Close()
    {
        if (Status != AllocationPeriodStatus.Open)
            throw new DomainException("Can only close an Open allocation period.");
        Status = AllocationPeriodStatus.Closed;
    }
}
