using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.RoommateGroups.Commands.LockGroup;

public record LockGroupCommand(Guid GroupId) : IRequest;

public class LockGroupCommandHandler(
    ICurrentUserService currentUser,
    IRoommateGroupRepository groupRepository,
    IPublisher publisher) : IRequestHandler<LockGroupCommand>
{
    public async Task Handle(LockGroupCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        var group = await groupRepository.FindByIdAsync(request.GroupId, cancellationToken)
            ?? throw new DomainException("Group not found.");
        if (group.LeaderUserId != userId)
            throw new DomainException("Only the leader can lock the group.");

        group.Lock();
        await groupRepository.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in group.DomainEvents)
            await publisher.Publish(domainEvent, cancellationToken);
        group.ClearDomainEvents();
    }
}
