namespace Aura.Application.AllocationPeriods.Common;

public record AllocationPeriodDto(Guid Id, string Name, DateTime StartDate, DateTime EndDate, string Status);
