using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aura.Application.DormAllocations.Events;

public class AllocationCreatedEmailHandler(
    IUserRepository userRepository,
    IDormitoryRepository dormitoryRepository,
    IDormAllocationRepository dormAllocationRepository,
    IAllocationPeriodRepository allocationPeriodRepository,
    IEmailService emailService,
    ILogger<AllocationCreatedEmailHandler> logger) : INotificationHandler<AllocationCreatedEvent>
{
    public async Task Handle(AllocationCreatedEvent notification, CancellationToken cancellationToken)
    {
        var allocation = await dormAllocationRepository.FindByIdAsync(notification.AllocationId, cancellationToken);
        var user = await userRepository.FindByIdAsync(notification.UserId, cancellationToken);
        var dorm = await dormitoryRepository.FindByIdWithCampusAsync(notification.DormitoryId, cancellationToken);
        var period = await allocationPeriodRepository.FindByIdAsync(notification.AllocationPeriodId, cancellationToken);

        if (allocation is null || user is null || string.IsNullOrWhiteSpace(user.Email)
            || dorm is null || dorm.Campus is null || period is null)
        {
            logger.LogWarning("Skipped placement email for allocation {AllocationId} — missing data.",
                notification.AllocationId);
            return;
        }

        // Compute respond-by from the persisted AllocatedAt rather than DateTime.UtcNow so that
        // retried/delayed handler runs don't shift the student's real response window.
        var respondBy = allocation.AllocatedAt.AddDays(period.ResponseWindowDays);
        await emailService.SendAllocationPlacedAsync(
            user.Email, user.FirstName,
            dorm.Name, dorm.Campus.Name, respondBy, cancellationToken);
    }
}
