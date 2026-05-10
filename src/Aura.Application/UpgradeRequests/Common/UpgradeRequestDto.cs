namespace Aura.Application.UpgradeRequests.Common;

public record UpgradeRequestDto(
    Guid Id,
    Guid AllocationPeriodId,
    DateTime CreatedAt,
    List<UpgradeTargetDto> Targets);

public record UpgradeTargetDto(
    int Rank,
    Guid DormitoryId,
    string DormitoryName,
    string CampusName);

public record AvailableUpgradeTargetDto(
    Guid DormitoryId,
    string DormitoryName,
    string CampusName);
