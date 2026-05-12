namespace Aura.Application.RoommateGroups.Common;

public record GroupDto(
    Guid Id,
    Guid AllocationPeriodId,
    Guid DormitoryId,
    string DormitoryName,
    Guid LeaderUserId,
    int RoomSizePreference,
    string Status,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? LockedAt,
    List<GroupMemberDto> Members);

public record GroupMemberDto(
    Guid UserId,
    string FirstName,
    string LastName,
    bool IsLeader);
