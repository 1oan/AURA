using Aura.Application.Common.Interfaces;
using MediatR;

namespace Aura.Application.RoommateGroups.Commands.ExpireOverdueGroups;

public record ExpireOverdueGroupsCommand : IRequest;

public class ExpireOverdueGroupsCommandHandler(
    IRoommateGroupRepository groupRepository,
    IGroupInvitationRepository invitationRepository,
    TimeProvider timeProvider,
    IPublisher publisher) : IRequestHandler<ExpireOverdueGroupsCommand>
{
    public async Task Handle(ExpireOverdueGroupsCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var overdue = await groupRepository.GetExpiredOverdueAsync(now, cancellationToken);
        if (overdue.Count == 0) return;

        foreach (var group in overdue)
        {
            group.Expire();

            var pending = await invitationRepository.GetPendingForGroupAsync(group.Id, cancellationToken);
            foreach (var invitation in pending)
                invitation.Expire();
        }

        await groupRepository.SaveChangesAsync(cancellationToken);
        await invitationRepository.SaveChangesAsync(cancellationToken);

        foreach (var group in overdue)
        {
            foreach (var domainEvent in group.DomainEvents)
                await publisher.Publish(domainEvent, cancellationToken);
            group.ClearDomainEvents();
        }
    }
}
