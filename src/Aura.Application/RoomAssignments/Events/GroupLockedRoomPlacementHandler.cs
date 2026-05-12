using Aura.Application.Common.Interfaces;
using Aura.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aura.Application.RoomAssignments.Events;

public class GroupLockedRoomPlacementHandler(
    IRoomAssignmentService roomAssignmentService,
    IRoomRepository roomRepository,
    IUserRepository userRepository,
    IDormitoryRepository dormitoryRepository,
    IEmailService emailService,
    ILogger<GroupLockedRoomPlacementHandler> logger) : INotificationHandler<GroupLockedEvent>
{
    public async Task Handle(GroupLockedEvent notification, CancellationToken cancellationToken)
    {
        var assignments = await roomAssignmentService.PlaceGroupAsync(notification.GroupId, cancellationToken);
        if (assignments.Count == 0)
        {
            logger.LogWarning("No assignments created for group {GroupId}", notification.GroupId);
            return;
        }

        var room = await roomRepository.FindByIdAsync(assignments[0].RoomId, cancellationToken);
        var dorm = await dormitoryRepository.FindByIdAsync(notification.DormitoryId, cancellationToken);
        var users = await userRepository.GetByIdsAsync([.. notification.MemberUserIds], cancellationToken);

        if (dorm is null || users.Count == 0)
        {
            logger.LogWarning("Skipped placement emails for group {GroupId} — missing data.", notification.GroupId);
            return;
        }

        var roomNumber = room?.Number ?? "N/A";
        var roommateFirstNames = users.Select(u => u.FirstName).ToArray();

        foreach (var user in users)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) continue;
            await emailService.SendPlacementConfirmationAsync(
                user.Email, user.FirstName, dorm.Name, roomNumber, roommateFirstNames, cancellationToken);
        }
    }
}
