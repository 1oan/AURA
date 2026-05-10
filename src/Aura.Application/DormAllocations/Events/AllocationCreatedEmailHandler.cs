using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aura.Application.DormAllocations.Events;

public class AllocationCreatedEmailHandler(
    IUserRepository userRepository,
    IDormitoryRepository dormitoryRepository,
    IAllocationPeriodRepository allocationPeriodRepository,
    IEmailService emailService,
    ILogger<AllocationCreatedEmailHandler> logger) : INotificationHandler<AllocationCreatedEvent>
{
    public async Task Handle(AllocationCreatedEvent notification, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(notification.UserId, cancellationToken);
        var dorm = await dormitoryRepository.FindByIdWithCampusAsync(notification.DormitoryId, cancellationToken);
        var period = await allocationPeriodRepository.FindByIdAsync(notification.AllocationPeriodId, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.Email) || dorm is null || dorm.Campus is null || period is null)
        {
            logger.LogWarning("Skipped placement email for allocation {AllocationId} — missing user/dorm/period.",
                notification.AllocationId);
            return;
        }

        var respondBy = DateTime.UtcNow.AddDays(period.ResponseWindowDays);
        await emailService.SendAllocationPlacedAsync(
            user.Email, user.FirstName,
            dorm.Name, dorm.Campus.Name, respondBy, cancellationToken);
    }
}
