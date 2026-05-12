using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aura.Application.DormAllocations.Events;

public class AllocationReplacedEmailHandler(
    IUserRepository userRepository,
    IDormitoryRepository dormitoryRepository,
    IEmailService emailService,
    ILogger<AllocationReplacedEmailHandler> logger) : INotificationHandler<AllocationReplacedEvent>
{
    public async Task Handle(AllocationReplacedEvent notification, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(notification.UserId, cancellationToken);
        var oldDorm = await dormitoryRepository.FindByIdAsync(notification.OldDormId, cancellationToken);
        var newDorm = await dormitoryRepository.FindByIdWithCampusAsync(notification.NewDormId, cancellationToken);

        if (user is null || string.IsNullOrWhiteSpace(user.Email) || oldDorm is null || newDorm is null || newDorm.Campus is null)
        {
            logger.LogWarning("Skipped upgrade email for user {UserId} — missing user/dorm.",
                notification.UserId);
            return;
        }

        await emailService.SendAllocationUpgradedAsync(
            user.Email, user.FirstName,
            oldDorm.Name, newDorm.Name, newDorm.Campus.Name, cancellationToken);
    }
}
