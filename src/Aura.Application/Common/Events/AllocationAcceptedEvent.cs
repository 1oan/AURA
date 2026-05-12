using MediatR;

namespace Aura.Application.Common.Events;

public record AllocationAcceptedEvent(
    Guid AllocationId,
    Guid UserId,
    Guid DormitoryId,
    Guid AllocationPeriodId) : INotification;
