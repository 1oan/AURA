using Aura.Application.Common.Interfaces;
using MediatR;

namespace Aura.Application.RoomAssignments.Queries.GetActivePeriodCountdown;

public record PeriodCountdownDto(
    Guid AllocationPeriodId,
    string PeriodName,
    DateTime ClosingAtUtc,
    double HoursRemaining);

public record GetActivePeriodCountdownQuery : IRequest<PeriodCountdownDto?>;

public class GetActivePeriodCountdownQueryHandler(
    IAllocationPeriodRepository periodRepository,
    TimeProvider timeProvider) : IRequestHandler<GetActivePeriodCountdownQuery, PeriodCountdownDto?>
{
    public async Task<PeriodCountdownDto?> Handle(GetActivePeriodCountdownQuery request, CancellationToken cancellationToken)
    {
        var period = await periodRepository.GetActiveAllocatingAsync(cancellationToken);
        if (period is null) return null;

        var hoursRemaining = (period.EndDate - timeProvider.GetUtcNow().UtcDateTime).TotalHours;

        return new PeriodCountdownDto(period.Id, period.Name, period.EndDate, hoursRemaining);
    }
}
