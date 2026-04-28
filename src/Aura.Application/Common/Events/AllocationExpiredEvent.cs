using MediatR;

namespace Aura.Application.Common.Events;

public record AllocationExpiredEvent(
    Guid AllocationId,
    Guid UserId,
    Guid DormitoryId,
    Guid AllocationPeriodId) : INotification;
