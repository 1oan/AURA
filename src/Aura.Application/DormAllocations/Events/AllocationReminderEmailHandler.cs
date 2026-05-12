using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aura.Application.DormAllocations.Events;

public class AllocationReminderEmailHandler(
    IUserRepository userRepository,
    IDormitoryRepository dormitoryRepository,
    IDormAllocationRepository dormAllocationRepository,
    IAllocationPeriodRepository allocationPeriodRepository,
    IEmailService emailService,
    ILogger<AllocationReminderEmailHandler> logger) : INotificationHandler<AllocationReminderDueEvent>
{
    public async Task Handle(AllocationReminderDueEvent notification, CancellationToken cancellationToken)
    {
        var allocation = await dormAllocationRepository.FindByIdAsync(notification.AllocationId, cancellationToken);
        var user = await userRepository.FindByIdAsync(notification.UserId, cancellationToken);
        var dorm = await dormitoryRepository.FindByIdWithCampusAsync(notification.DormitoryId, cancellationToken);
        var period = await allocationPeriodRepository.FindByIdAsync(notification.AllocationPeriodId, cancellationToken);

        if (allocation is null || user is null || string.IsNullOrWhiteSpace(user.Email)
            || dorm is null || dorm.Campus is null || period is null)
        {
            logger.LogWarning("Skipped reminder email for allocation {AllocationId} — missing data.",
                notification.AllocationId);
            return;
        }

        var respondBy = allocation.AllocatedAt.AddDays(period.ResponseWindowDays);
        await emailService.SendAllocationReminderAsync(
            user.Email, user.FirstName,
            dorm.Name, dorm.Campus.Name, respondBy, cancellationToken);
    }
}
