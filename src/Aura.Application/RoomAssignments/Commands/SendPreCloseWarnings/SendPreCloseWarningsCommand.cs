using Aura.Application.Common.Interfaces;
using MediatR;

namespace Aura.Application.RoomAssignments.Commands.SendPreCloseWarnings;

public record SendPreCloseWarningsCommand : IRequest<Unit>;

public class SendPreCloseWarningsCommandHandler(
    IAllocationPeriodRepository periodRepository,
    IDormAllocationRepository dormAllocationRepository,
    IUserRepository userRepository,
    IEmailService emailService,
    TimeProvider timeProvider) : IRequestHandler<SendPreCloseWarningsCommand, Unit>
{
    public async Task<Unit> Handle(SendPreCloseWarningsCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var periods = await periodRepository.ListAllocatingAsync(cancellationToken);

        foreach (var period in periods)
        {
            var hoursRemaining = (period.EndDate - now).TotalHours;

            if (!IsAtCheckpoint(hoursRemaining))
                continue;

            var candidates = await dormAllocationRepository.ListAcceptedWithoutRoomAsync(period.Id, cancellationToken);
            var due = candidates
                .Where(a => a.LastPreCloseWarningSentAt is null
                            || (now - a.LastPreCloseWarningSentAt.Value).TotalHours > 20)
                .ToList();

            if (due.Count == 0)
                continue;

            foreach (var alloc in due)
                alloc.MarkPreCloseWarningSent();

            await dormAllocationRepository.SaveChangesAsync(cancellationToken);

            var userIds = due.Select(a => a.UserId).ToList();
            var users = await userRepository.GetByIdsAsync(userIds, cancellationToken);

            foreach (var alloc in due)
            {
                var user = users.FirstOrDefault(u => u.Id == alloc.UserId);
                if (user is null || string.IsNullOrWhiteSpace(user.Email))
                    continue;

                var dormName = alloc.Dormitory?.Name ?? "your dormitory";
                await emailService.SendPreCloseWarningAsync(
                    user.Email, user.FirstName, dormName, period.EndDate, cancellationToken);
            }
        }

        return Unit.Value;
    }

    private static bool IsAtCheckpoint(double hoursRemaining) =>
        hoursRemaining is (>= 72 and < 73) or (>= 48 and < 49) or (>= 24 and < 25);
}
