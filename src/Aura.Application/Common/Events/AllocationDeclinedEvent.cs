using MediatR;

namespace Aura.Application.Common.Events;

public record AllocationDeclinedEvent(
    Guid AllocationId,
    Guid UserId,
    Guid DormitoryId,
    Guid AllocationPeriodId) : INotification;
