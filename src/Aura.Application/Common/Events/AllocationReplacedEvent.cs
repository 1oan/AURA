using MediatR;

namespace Aura.Application.Common.Events;

public record AllocationReplacedEvent(
    Guid UserId,
    Guid OldDormId,
    Guid NewDormId,
    Guid AllocationPeriodId) : INotification;
