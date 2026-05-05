using Aura.Application.Common.Interfaces;
using Aura.Application.DormPreferences.Common;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.DormPreferences.Queries.GetAvailableDormitories;

public record GetAvailableDormitoriesQuery(Guid AllocationPeriodId) : IRequest<List<AvailableDormitoryDto>>;

public class GetAvailableDormitoriesQueryHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IAllocationPeriodRepository allocationPeriodRepository,
    IFacultyRoomAllocationRepository facultyRoomAllocationRepository,
    IDormAllocationRepository dormAllocationRepository) : IRequestHandler<GetAvailableDormitoriesQuery, List<AvailableDormitoryDto>>
{
    public async Task<List<AvailableDormitoryDto>> Handle(GetAvailableDormitoriesQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.FacultyId is null || user.Gender is null)
            throw new DomainException("You must participate in the allocation period before viewing available dormitories.");

        var period = await allocationPeriodRepository.FindByIdAsync(query.AllocationPeriodId, cancellationToken)
            ?? throw new NotFoundException("Allocation period not found.");

        if (period.Status != AllocationPeriodStatus.Open && period.Status != AllocationPeriodStatus.Allocating)
            throw new DomainException("Allocation period is not accepting preference submissions.");

        if (await dormAllocationRepository.HasTerminalForUserAndPeriodAsync(userId, query.AllocationPeriodId, cancellationToken))
            throw new DomainException("You are no longer eligible to submit preferences for this period.");

        var allocations = await facultyRoomAllocationRepository
            .GetByPeriodAndFacultyAsync(query.AllocationPeriodId, user.FacultyId.Value, cancellationToken);

        // Filter rooms by student's gender, group by dormitory, aggregate spots
        return allocations
            .Where(a => a.Room.Gender == user.Gender.Value)
            .GroupBy(a => new { a.Room.DormitoryId, a.Room.Dormitory.Name, CampusName = a.Room.Dormitory.Campus.Name })
            .Select(g => new AvailableDormitoryDto(
                g.Key.DormitoryId,
                g.Key.Name,
                g.Key.CampusName,
                g.Sum(a => a.Room.Capacity)))
            .OrderBy(d => d.CampusName)
            .ThenBy(d => d.DormitoryName)
            .ToList();
    }
}
