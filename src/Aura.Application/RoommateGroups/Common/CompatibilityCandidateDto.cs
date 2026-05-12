namespace Aura.Application.RoommateGroups.Common;

public record CompatibilityCandidateDto(
    Guid UserId,
    string FirstName,
    string LastName,
    decimal Score,
    string[] SignalsUsed);
