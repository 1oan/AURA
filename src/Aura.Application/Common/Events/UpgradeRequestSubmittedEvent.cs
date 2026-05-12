using MediatR;

namespace Aura.Application.Common.Events;

public record UpgradeRequestSubmittedEvent(
    Guid UpgradeRequestId,
    Guid UserId,
    Guid AllocationPeriodId,
    IReadOnlyList<Guid> TargetDormIds) : INotification;
