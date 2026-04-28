using Aura.Application.Common.Interfaces;
using Aura.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Aura.Application.Dashboard.Queries.GetDashboardStats;

public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public class GetDashboardStatsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    public async Task<DashboardStatsDto> Handle(
        GetDashboardStatsQuery request,
        CancellationToken cancellationToken)
    {
        var campusCount = await dbContext.Campuses.CountAsync(cancellationToken);
        var dormitoryCount = await dbContext.Dormitories.CountAsync(cancellationToken);
        var roomStats = await dbContext.Rooms
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), TotalCapacity = g.Sum(r => r.Capacity) })
            .FirstOrDefaultAsync(cancellationToken);
        var facultyCount = await dbContext.Faculties.CountAsync(cancellationToken);
        var activePeriod = await dbContext.AllocationPeriods
            .FirstOrDefaultAsync(p => p.Status == AllocationPeriodStatus.Open || p.Status == AllocationPeriodStatus.Allocating, cancellationToken);

        ActivePeriodDto? activePeriodDto = activePeriod is not null
            ? new ActivePeriodDto(
                activePeriod.Id,
                activePeriod.Name,
                activePeriod.Status.ToString(),
                activePeriod.StartDate,
                activePeriod.EndDate)
            : null;

        var allocationsByFaculty = new List<FacultyAllocationDto>();

        if (activePeriod is not null)
        {
            var counts = await dbContext.FacultyRoomAllocations
                .Where(a => a.AllocationPeriodId == activePeriod.Id)
                .GroupBy(a => a.FacultyId)
                .Select(g => new { FacultyId = g.Key, RoomCount = g.Count() })
                .ToListAsync(cancellationToken);

            var facultyIds = counts.Select(c => c.FacultyId).ToList();
            var facultiesById = await dbContext.Faculties
                .Where(f => facultyIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, cancellationToken);

            allocationsByFaculty = counts
                .Select(c => new FacultyAllocationDto(
                    c.FacultyId,
                    facultiesById.TryGetValue(c.FacultyId, out var fac) ? fac.Name : string.Empty,
                    facultiesById.TryGetValue(c.FacultyId, out var fac2) ? fac2.Abbreviation : string.Empty,
                    c.RoomCount))
                .OrderByDescending(f => f.RoomCount)
                .ToList();
        }

        return new DashboardStatsDto(
            campusCount,
            dormitoryCount,
            roomStats?.Count ?? 0,
            roomStats?.TotalCapacity ?? 0,
            facultyCount,
            activePeriodDto,
            allocationsByFaculty);
    }
}