using Aura.Application.Common.Interfaces;
using Aura.Application.UpgradeRequests.Common;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.UpgradeRequests.Queries.GetMyUpgradeRequest;

public record GetMyUpgradeRequestQuery(Guid AllocationPeriodId) : IRequest<UpgradeRequestDto?>;

public class GetMyUpgradeRequestQueryHandler(
    ICurrentUserService currentUserService,
    IUpgradeRequestRepository upgradeRequestRepository,
    IUserRepository userRepository,
    IFacultyRoomAllocationRepository facultyRoomAllocationRepository) : IRequestHandler<GetMyUpgradeRequestQuery, UpgradeRequestDto?>
{
    public async Task<UpgradeRequestDto?> Handle(GetMyUpgradeRequestQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var upgrade = await upgradeRequestRepository.FindActiveByUserAndPeriodAsync(
            userId, request.AllocationPeriodId, cancellationToken);

        if (upgrade is null)
            return null;

        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        // Edge case: an admin detached the student's faculty after they had submitted an
        // upgrade request. Fall back to empty dorm-name lookup so the UI can still render
        // the existing request rather than 400-ing the user out.
        var dormitoryLookup = user.FacultyId is null
            ? new Dictionary<Guid, (string DormitoryName, string CampusName)>()
            : (await facultyRoomAllocationRepository
                .GetByPeriodAndFacultyAsync(request.AllocationPeriodId, user.FacultyId.Value, cancellationToken))
                .GroupBy(a => a.Room.DormitoryId)
                .ToDictionary(
                    g => g.Key,
                    g => (
                        DormitoryName: g.First().Room.Dormitory.Name,
                        CampusName: g.First().Room.Dormitory.Campus.Name));

        var targets = upgrade.Targets
            .OrderBy(t => t.Rank)
            .Select(t =>
            {
                var found = dormitoryLookup.TryGetValue(t.DormitoryId, out var info);
                return new UpgradeTargetDto(
                    t.Rank,
                    t.DormitoryId,
                    found ? info.DormitoryName : string.Empty,
                    found ? info.CampusName : string.Empty);
            })
            .ToList();

        return new UpgradeRequestDto(
            upgrade.Id,
            upgrade.AllocationPeriodId,
            upgrade.CreatedAt,
            targets);
    }
}
