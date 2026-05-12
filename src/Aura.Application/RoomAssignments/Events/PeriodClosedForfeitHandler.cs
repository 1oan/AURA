using Aura.Application.Common.Interfaces;
using Aura.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aura.Application.RoomAssignments.Events;

public class PeriodClosedForfeitHandler(
    IRoomAssignmentService roomAssignmentService,
    IUserRepository userRepository,
    IDormAllocationRepository dormAllocationRepository,
    IDormitoryRepository dormitoryRepository,
    IEmailService emailService,
    ILogger<PeriodClosedForfeitHandler> logger) : INotificationHandler<AllocationPeriodClosedEvent>
{
    public async Task Handle(AllocationPeriodClosedEvent notification, CancellationToken cancellationToken)
    {
        var forfeitedUserIds = await roomAssignmentService.ForfeitNonCommittedAsync(
            notification.AllocationPeriodId, cancellationToken);

        if (forfeitedUserIds.Count == 0)
            return;

        var users = await userRepository.GetByIdsAsync(forfeitedUserIds, cancellationToken);

        foreach (var user in users)
        {
            var allocation = await dormAllocationRepository.FindLatestByUserAndPeriodAsync(
                user.Id, notification.AllocationPeriodId, cancellationToken);
            var dormName = allocation?.Dormitory?.Name ?? "your dormitory";

            if (string.IsNullOrWhiteSpace(user.Email)) continue;

            await emailService.SendForfeitedNotificationAsync(
                user.Email, user.FirstName, dormName, cancellationToken);
        }

        logger.LogInformation("Forfeited {Count} allocations for period {PeriodId}",
            forfeitedUserIds.Count, notification.AllocationPeriodId);
    }
}
