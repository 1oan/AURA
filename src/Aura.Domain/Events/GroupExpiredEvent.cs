using MediatR;

namespace Aura.Domain.Events;

public record GroupExpiredEvent(
    Guid GroupId,
    Guid AllocationPeriodId,
    Guid DormitoryId,
    Guid[] MemberUserIds) : INotification;
