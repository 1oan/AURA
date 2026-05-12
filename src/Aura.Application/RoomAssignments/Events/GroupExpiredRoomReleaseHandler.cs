using Aura.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aura.Application.RoomAssignments.Events;

public class GroupExpiredRoomReleaseHandler(
    ILogger<GroupExpiredRoomReleaseHandler> logger) : INotificationHandler<GroupExpiredEvent>
{
    public Task Handle(GroupExpiredEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Group {GroupId} expired; no room assignment to release.", notification.GroupId);
        return Task.CompletedTask;
    }
}
