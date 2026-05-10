using Aura.Application.Common.Events;
using Aura.Application.UpgradeRequests.Services;
using MediatR;

namespace Aura.Application.DormAllocations.Events;

public class CapacityFreedNotificationHandler(IUpgradeFulfillmentService fulfillmentService)
    : INotificationHandler<AllocationDeclinedEvent>,
      INotificationHandler<AllocationExpiredEvent>,
      INotificationHandler<AllocationReplacedEvent>
{
    public Task Handle(AllocationDeclinedEvent notification, CancellationToken cancellationToken) =>
        fulfillmentService.TryFulfillForDormAsync(
            notification.DormitoryId, notification.AllocationPeriodId, cancellationToken);

    public Task Handle(AllocationExpiredEvent notification, CancellationToken cancellationToken) =>
        fulfillmentService.TryFulfillForDormAsync(
            notification.DormitoryId, notification.AllocationPeriodId, cancellationToken);

    public Task Handle(AllocationReplacedEvent notification, CancellationToken cancellationToken) =>
        fulfillmentService.TryFulfillForDormAsync(
            notification.OldDormId, notification.AllocationPeriodId, cancellationToken);
}
