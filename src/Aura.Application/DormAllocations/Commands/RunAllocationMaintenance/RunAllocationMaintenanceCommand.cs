using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using MediatR;

namespace Aura.Application.DormAllocations.Commands.RunAllocationMaintenance;

public record RunAllocationMaintenanceCommand : IRequest;

public class RunAllocationMaintenanceCommandHandler(
    IDormAllocationRepository dormAllocationRepository,
    IPublisher publisher,
    TimeProvider timeProvider) : IRequestHandler<RunAllocationMaintenanceCommand>
{
    public async Task Handle(RunAllocationMaintenanceCommand _, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        await SendHalfwayPointReminders(now, cancellationToken);
        await ExpireOverdue(now, cancellationToken);
    }

    private async Task SendHalfwayPointReminders(DateTime now, CancellationToken cancellationToken)
    {
        var due = await dormAllocationRepository.GetReminderDueAsync(now, cancellationToken);
        if (due.Count == 0) return;

        foreach (var allocation in due)
        {
            allocation.MarkReminderSent();
        }
        await dormAllocationRepository.SaveChangesAsync(cancellationToken);

        foreach (var allocation in due)
        {
            await publisher.Publish(
                new AllocationReminderDueEvent(allocation.Id, allocation.UserId, allocation.DormitoryId, allocation.AllocationPeriodId),
                cancellationToken);
        }
    }

    private async Task ExpireOverdue(DateTime now, CancellationToken cancellationToken)
    {
        var stale = await dormAllocationRepository.GetExpirablePendingAsync(now, cancellationToken);
        if (stale.Count == 0) return;

        foreach (var allocation in stale)
        {
            allocation.Expire();
        }
        await dormAllocationRepository.SaveChangesAsync(cancellationToken);

        foreach (var allocation in stale)
        {
            await publisher.Publish(
                new AllocationExpiredEvent(allocation.Id, allocation.UserId, allocation.DormitoryId, allocation.AllocationPeriodId),
                cancellationToken);
        }
    }
}
