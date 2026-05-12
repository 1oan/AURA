using Aura.Application.Common.Interfaces;
using Aura.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aura.Application.RoommateGroups.Events;

public class GroupExpiredEmailHandler(
    IUserRepository userRepository,
    IDormitoryRepository dormitoryRepository,
    IEmailService emailService,
    ILogger<GroupExpiredEmailHandler> logger) : INotificationHandler<GroupExpiredEvent>
{
    public async Task Handle(GroupExpiredEvent notification, CancellationToken cancellationToken)
    {
        var dorm = await dormitoryRepository.FindByIdAsync(notification.DormitoryId, cancellationToken);
        var users = await userRepository.GetByIdsAsync([.. notification.MemberUserIds], cancellationToken);
        if (dorm is null || users.Count == 0)
        {
            logger.LogWarning("Skipped group-expired emails for group {GroupId} — missing data.", notification.GroupId);
            return;
        }

        foreach (var user in users)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) continue;
            await emailService.SendGroupExpiredAsync(user.Email, user.FirstName, dorm.Name, cancellationToken);
        }
    }
}
