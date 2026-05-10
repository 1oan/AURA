using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aura.Application.DormAllocations.Events;

public class AllocationExpiredEmailHandler(
    IUserRepository userRepository,
    IDormitoryRepository dormitoryRepository,
    IEmailService emailService,
    ILogger<AllocationExpiredEmailHandler> logger) : INotificationHandler<AllocationExpiredEvent>
{
    public async Task Handle(AllocationExpiredEvent notification, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(notification.UserId, cancellationToken);
        var dorm = await dormitoryRepository.FindByIdAsync(notification.DormitoryId, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.Email) || dorm is null)
        {
            logger.LogWarning("Skipped expired email for allocation {AllocationId} — missing user/dorm.",
                notification.AllocationId);
            return;
        }

        await emailService.SendAllocationExpiredAsync(
            user.Email, user.FirstName, dorm.Name, cancellationToken);
    }
}
