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
            allocationsByFaculty = await dbContext.FacultyRoomAllocations
                .Where(a => a.AllocationPeriodId == activePeriod.Id)
                .GroupBy(a => new { a.FacultyId, a.Faculty.Name, a.Faculty.Abbreviation })
                .Select(g => new FacultyAllocationDto(
                    g.Key.FacultyId,
                    g.Key.Name,
                    g.Key.Abbreviation,
                    g.Count()))
                .OrderByDescending(f => f.RoomCount)
                .ToListAsync(cancellationToken);
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