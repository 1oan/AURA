using MediatR;

namespace Aura.Domain.Events;

public record AllocationPeriodClosedEvent(Guid AllocationPeriodId) : INotification;
