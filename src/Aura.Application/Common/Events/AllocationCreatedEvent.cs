using MediatR;

namespace Aura.Application.Common.Events;

public record AllocationCreatedEvent(
    Guid AllocationId,
    Guid UserId,
    Guid DormitoryId,
    Guid AllocationPeriodId,
    int Round) : INotification;
