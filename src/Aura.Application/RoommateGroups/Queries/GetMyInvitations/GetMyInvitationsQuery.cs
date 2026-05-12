using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Common;
using MediatR;

namespace Aura.Application.RoommateGroups.Queries.GetMyInvitations;

public record GetMyInvitationsQuery : IRequest<List<InvitationDto>>;

public class GetMyInvitationsQueryHandler(
    ICurrentUserService currentUser,
    IGroupInvitationRepository invitationRepository,
    IRoommateGroupRepository groupRepository,
    IDormitoryRepository dormitoryRepository,
    IUserRepository userRepository) : IRequestHandler<GetMyInvitationsQuery, List<InvitationDto>>
{
    public async Task<List<InvitationDto>> Handle(GetMyInvitationsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        var invitations = await invitationRepository.GetPendingForUserAsync(userId, cancellationToken);
        if (invitations.Count == 0) return [];

        var groupIds = invitations.Select(i => i.GroupId).Distinct().ToList();
        var inviterIds = invitations.Select(i => i.InviterUserId).Distinct().ToList();

        var groups = new Dictionary<Guid, Domain.Entities.RoommateGroup>();
        foreach (var groupId in groupIds)
        {
            var group = await groupRepository.FindByIdAsync(groupId, cancellationToken);
            if (group is not null) groups[groupId] = group;
        }

        var dormIds = groups.Values.Select(g => g.DormitoryId).Distinct().ToList();
        var dorms = new Dictionary<Guid, string>();
        foreach (var dormId in dormIds)
        {
            var dorm = await dormitoryRepository.FindByIdAsync(dormId, cancellationToken);
            if (dorm is not null) dorms[dormId] = dorm.Name;
        }

        var inviters = await userRepository.GetByIdsAsync(inviterIds, cancellationToken);
        var invitersById = inviters.ToDictionary(u => u.Id);

        return invitations
            .Where(i => groups.ContainsKey(i.GroupId) && invitersById.ContainsKey(i.InviterUserId))
            .Select(i => new InvitationDto(
                i.Id, i.GroupId,
                dorms.GetValueOrDefault(groups[i.GroupId].DormitoryId, "Unknown"),
                (int)groups[i.GroupId].RoomSizePreference,
                i.InviterUserId,
                invitersById[i.InviterUserId].FirstName,
                invitersById[i.InviterUserId].LastName,
                i.CreatedAt))
            .ToList();
    }
}
