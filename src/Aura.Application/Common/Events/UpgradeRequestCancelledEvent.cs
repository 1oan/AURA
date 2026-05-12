using MediatR;

namespace Aura.Application.Common.Events;

public record UpgradeRequestCancelledEvent(
    Guid UpgradeRequestId,
    Guid UserId,
    Guid AllocationPeriodId) : INotification;
