using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Common;
using MediatR;

namespace Aura.Application.RoommateGroups.Queries.GetMyGroup;

public record GetMyGroupQuery : IRequest<GroupDto?>;

public class GetMyGroupQueryHandler(
    ICurrentUserService currentUser,
    IRoommateGroupRepository groupRepository,
    IDormitoryRepository dormitoryRepository,
    IUserRepository userRepository,
    IAllocationPeriodRepository periodRepository) : IRequestHandler<GetMyGroupQuery, GroupDto?>
{
    public async Task<GroupDto?> Handle(GetMyGroupQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        var periods = await periodRepository.GetActiveAsync(cancellationToken);
        var period = periods.FirstOrDefault();
        if (period is null) return null;

        var group = await groupRepository.FindActiveByUserAndPeriodAsync(userId, period.Id, cancellationToken);
        if (group is null) return null;

        var dorm = await dormitoryRepository.FindByIdAsync(group.DormitoryId, cancellationToken);
        var memberIds = group.Members.Select(m => m.UserId).ToList();
        var users = await userRepository.GetByIdsAsync(memberIds, cancellationToken);

        var memberDtos = users
            .Select(u => new GroupMemberDto(u.Id, u.FirstName, u.LastName, u.Id == group.LeaderUserId))
            .ToList();

        return new GroupDto(
            group.Id, group.AllocationPeriodId, group.DormitoryId,
            dorm?.Name ?? "Unknown", group.LeaderUserId,
            (int)group.RoomSizePreference, group.Status.ToString(),
            group.CreatedAt, group.ExpiresAt, group.LockedAt, memberDtos);
    }
}
