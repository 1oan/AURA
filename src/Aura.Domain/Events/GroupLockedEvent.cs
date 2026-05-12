using MediatR;

namespace Aura.Domain.Events;

public record GroupLockedEvent(
    Guid GroupId,
    Guid AllocationPeriodId,
    Guid DormitoryId,
    Guid LeaderUserId,
    int RoomSizePreference,
    Guid[] MemberUserIds) : INotification;
