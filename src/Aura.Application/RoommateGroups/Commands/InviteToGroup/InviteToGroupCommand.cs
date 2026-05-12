using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.RoommateGroups.Commands.InviteToGroup;

public record InviteToGroupCommand(Guid GroupId, Guid InviteeUserId) : IRequest<Guid>;

public class InviteToGroupCommandHandler(
    ICurrentUserService currentUser,
    IRoommateGroupRepository groupRepository,
    IGroupInvitationRepository invitationRepository,
    IDormAllocationRepository allocationRepository,
    IUserRepository userRepository,
    IPublisher publisher) : IRequestHandler<InviteToGroupCommand, Guid>
{
    public async Task<Guid> Handle(InviteToGroupCommand request, CancellationToken cancellationToken)
    {
        var leaderId = currentUser.GetCurrentUserId();

        var group = await groupRepository.FindByIdAsync(request.GroupId, cancellationToken)
            ?? throw new DomainException("Group not found.");
        if (group.LeaderUserId != leaderId)
            throw new DomainException("Only the group leader can invite members.");
        if (group.Status != GroupStatus.Forming)
            throw new DomainException("Cannot invite to a group that is not Forming.");
        if (group.Members.Count >= (int)group.RoomSizePreference)
            throw new DomainException("Group is at capacity.");
        if (group.Members.Any(m => m.UserId == request.InviteeUserId))
            throw new DomainException("Invitee is already a member of this group.");

        var leader = await userRepository.FindByIdAsync(leaderId, cancellationToken)
            ?? throw new DomainException("Leader user not found.");
        var invitee = await userRepository.FindByIdAsync(request.InviteeUserId, cancellationToken)
            ?? throw new DomainException("Invitee user not found.");

        if (invitee.Gender != leader.Gender)
            throw new DomainException("Invitee must be the same gender.");

        var inviteeAllocation = await allocationRepository.FindActiveByUserAndPeriodAsync(
            request.InviteeUserId, group.AllocationPeriodId, cancellationToken)
            ?? throw new DomainException("Invitee does not have an active allocation in this period.");
        if (inviteeAllocation.Status != AllocationStatus.Accepted)
            throw new DomainException("Invitee must have an Accepted allocation.");
        if (inviteeAllocation.DormitoryId != group.DormitoryId)
            throw new DomainException("Invitee is allocated to a different dorm.");

        var inviteeGroup = await groupRepository.FindActiveByUserAndPeriodAsync(
            request.InviteeUserId, group.AllocationPeriodId, cancellationToken);
        if (inviteeGroup is not null)
            throw new DomainException("Invitee is already in another active group.");

        var existingPending = await invitationRepository.FindPendingAsync(
            group.Id, request.InviteeUserId, cancellationToken);
        if (existingPending is not null)
            throw new DomainException("Invitee already has a pending invitation to this group.");

        var invitation = GroupInvitation.Create(group.Id, leaderId, request.InviteeUserId);
        await invitationRepository.AddAsync(invitation, cancellationToken);
        await invitationRepository.SaveChangesAsync(cancellationToken);

        await publisher.Publish(new GroupInvitationCreatedEvent(
            invitation.Id, request.InviteeUserId, leaderId, group.Id), cancellationToken);

        return invitation.Id;
    }
}
