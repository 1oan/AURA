using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.RoommateGroups.Commands.LeaveGroup;

public record LeaveGroupCommand(Guid GroupId) : IRequest;

public class LeaveGroupCommandHandler(
    ICurrentUserService currentUser,
    IRoommateGroupRepository groupRepository) : IRequestHandler<LeaveGroupCommand>
{
    public async Task Handle(LeaveGroupCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        var group = await groupRepository.FindByIdAsync(request.GroupId, cancellationToken)
            ?? throw new DomainException("Group not found.");
        if (group.Members.All(m => m.UserId != userId))
            throw new DomainException("You are not a member of this group.");

        group.RemoveMember(userId);
        await groupRepository.SaveChangesAsync(cancellationToken);
    }
}
