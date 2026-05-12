using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aura.Application.RoommateGroups.Events;

public class GroupInvitationEmailHandler(
    IUserRepository userRepository,
    IRoommateGroupRepository groupRepository,
    IDormitoryRepository dormitoryRepository,
    IEmailService emailService,
    ILogger<GroupInvitationEmailHandler> logger) : INotificationHandler<GroupInvitationCreatedEvent>
{
    public async Task Handle(GroupInvitationCreatedEvent notification, CancellationToken cancellationToken)
    {
        var invitee = await userRepository.FindByIdAsync(notification.InviteeUserId, cancellationToken);
        var inviter = await userRepository.FindByIdAsync(notification.InviterUserId, cancellationToken);
        var group = await groupRepository.FindByIdAsync(notification.GroupId, cancellationToken);
        var dorm = group is null ? null : await dormitoryRepository.FindByIdAsync(group.DormitoryId, cancellationToken);

        if (invitee is null || string.IsNullOrWhiteSpace(invitee.Email)
            || inviter is null || group is null || dorm is null)
        {
            logger.LogWarning("Skipped invitation email for {InvitationId} — missing data.", notification.InvitationId);
            return;
        }

        await emailService.SendGroupInvitationAsync(
            invitee.Email, invitee.FirstName, inviter.FirstName,
            dorm.Name, (int)group.RoomSizePreference, cancellationToken);
    }
}
