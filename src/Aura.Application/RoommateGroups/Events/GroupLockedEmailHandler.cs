using Aura.Application.Common.Interfaces;
using Aura.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aura.Application.RoommateGroups.Events;

public class GroupLockedEmailHandler(
    IUserRepository userRepository,
    IDormitoryRepository dormitoryRepository,
    IEmailService emailService,
    ILogger<GroupLockedEmailHandler> logger) : INotificationHandler<GroupLockedEvent>
{
    public async Task Handle(GroupLockedEvent notification, CancellationToken cancellationToken)
    {
        var dorm = await dormitoryRepository.FindByIdAsync(notification.DormitoryId, cancellationToken);
        var users = await userRepository.GetByIdsAsync([.. notification.MemberUserIds], cancellationToken);
        if (dorm is null || users.Count == 0)
        {
            logger.LogWarning("Skipped group-locked emails for group {GroupId} — missing data.", notification.GroupId);
            return;
        }

        var memberFirstNames = users.Select(u => u.FirstName).ToArray();
        foreach (var user in users)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) continue;
            await emailService.SendGroupLockedAsync(
                user.Email, user.FirstName, dorm.Name, memberFirstNames, cancellationToken);
        }
    }
}
