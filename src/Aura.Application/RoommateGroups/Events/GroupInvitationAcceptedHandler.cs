using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aura.Application.RoommateGroups.Events;

public class GroupInvitationAcceptedHandler(
    IRoommateGroupRepository groupRepository,
    ILogger<GroupInvitationAcceptedHandler> logger) : INotificationHandler<GroupInvitationAcceptedEvent>
{
    public async Task Handle(GroupInvitationAcceptedEvent notification, CancellationToken cancellationToken)
    {
        var group = await groupRepository.FindByIdAsync(notification.GroupId, cancellationToken);
        if (group is null)
        {
            logger.LogWarning("Group {GroupId} not found while handling accepted invitation.", notification.GroupId);
            return;
        }

        group.AddMember(notification.InviteeUserId);
        await groupRepository.SaveChangesAsync(cancellationToken);
    }
}
