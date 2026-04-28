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
    public DateTime Round1Date { get; private set; }
    public int ResponseWindowDays { get; private set; }

    private AllocationPeriod() { }

    public static AllocationPeriod Create(string name, DateTime startDate, DateTime endDate, DateTime round1Date, int responseWindowDays)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Allocation period name is required.");
        if (name.Length > 200)
            throw new DomainException("Allocation period name must not exceed 200 characters.");
        if (endDate <= startDate)
            throw new DomainException("End date must be after start date.");
        if (round1Date < startDate || round1Date > endDate)
            throw new DomainException("Round 1 date must fall within the allocation period.");
        if (responseWindowDays < 1)
            throw new DomainException("Response window must be at least 1 day.");

        return new AllocationPeriod
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc),
            Status = AllocationPeriodStatus.Draft,
            Round1Date = DateTime.SpecifyKind(round1Date, DateTimeKind.Utc),
            ResponseWindowDays = responseWindowDays,
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
        StartDate = DateTime.SpecifyKind(startDate, DateTimeKind.Utc);
        EndDate = DateTime.SpecifyKind(endDate, DateTimeKind.Utc);
    }

    public void Activate()
    {
        if (Status != AllocationPeriodStatus.Draft)
            throw new DomainException("Can only activate a Draft allocation period.");
        Status = AllocationPeriodStatus.Open;
    }

    public void StartAllocating()
    {
        if (Status != AllocationPeriodStatus.Open)
            throw new DomainException("Can only start allocating an Open allocation period.");
        Status = AllocationPeriodStatus.Allocating;
    }

    public void Close()
    {
        if (Status != AllocationPeriodStatus.Allocating)
            throw new DomainException("Can only close an Allocating allocation period.");
        Status = AllocationPeriodStatus.Closed;
    }
}
