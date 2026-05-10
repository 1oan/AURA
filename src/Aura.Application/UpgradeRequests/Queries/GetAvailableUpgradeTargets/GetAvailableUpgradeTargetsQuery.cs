using Aura.Application.Common.Interfaces;
using Aura.Application.UpgradeRequests.Common;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.UpgradeRequests.Queries.GetAvailableUpgradeTargets;

public record GetAvailableUpgradeTargetsQuery(Guid AllocationPeriodId) : IRequest<List<AvailableUpgradeTargetDto>>;

public class GetAvailableUpgradeTargetsQueryHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IAllocationPeriodRepository allocationPeriodRepository,
    IFacultyRoomAllocationRepository facultyRoomAllocationRepository,
    IDormAllocationRepository dormAllocationRepository) : IRequestHandler<GetAvailableUpgradeTargetsQuery, List<AvailableUpgradeTargetDto>>
{
    public async Task<List<AvailableUpgradeTargetDto>> Handle(GetAvailableUpgradeTargetsQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.FacultyId is null || user.Gender is null)
            throw new DomainException("You must participate in the allocation period before viewing upgrade targets.");

        var period = await allocationPeriodRepository.FindByIdAsync(query.AllocationPeriodId, cancellationToken)
            ?? throw new NotFoundException("Allocation period not found.");

        if (period.Status != AllocationPeriodStatus.Open && period.Status != AllocationPeriodStatus.Allocating)
            throw new DomainException("Allocation period is not accepting upgrade requests.");

        var current = await dormAllocationRepository.FindActiveByUserAndPeriodAsync(userId, query.AllocationPeriodId, cancellationToken)
            ?? throw new DomainException("You must have an active allocation to view upgrade targets.");

        var allocations = await facultyRoomAllocationRepository
            .GetByPeriodAndFacultyAsync(query.AllocationPeriodId, user.FacultyId.Value, cancellationToken);

        return allocations
            .Where(a => a.Room.Gender == user.Gender.Value)
            .Where(a => a.Room.DormitoryId != current.DormitoryId)
            .GroupBy(a => new { a.Room.DormitoryId, a.Room.Dormitory.Name, CampusName = a.Room.Dormitory.Campus.Name })
            .Select(g => new AvailableUpgradeTargetDto(
                g.Key.DormitoryId,
                g.Key.Name,
                g.Key.CampusName))
            .OrderBy(d => d.CampusName)
            .ThenBy(d => d.DormitoryName)
            .ToList();
    }
}
