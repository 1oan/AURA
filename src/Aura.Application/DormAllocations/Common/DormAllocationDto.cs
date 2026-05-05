namespace Aura.Application.DormAllocations.Common;

public record DormAllocationDto(
    Guid Id,
    Guid UserId,
    string? UserFirstName,
    string? UserLastName,
    string? UserMatriculationCode,
    Guid DormitoryId,
    string DormitoryName,
    string CampusName,
    Guid AllocationPeriodId,
    int Round,
    string Status,
    DateTime AllocatedAt,
    DateTime? RespondedAt);
