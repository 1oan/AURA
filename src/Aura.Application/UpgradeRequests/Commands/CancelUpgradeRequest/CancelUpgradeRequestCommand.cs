using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.UpgradeRequests.Commands.CancelUpgradeRequest;

public record CancelUpgradeRequestCommand(Guid UpgradeRequestId) : IRequest;

public class CancelUpgradeRequestCommandHandler(
    ICurrentUserService currentUserService,
    IUpgradeRequestRepository upgradeRequestRepository,
    IPublisher publisher) : IRequestHandler<CancelUpgradeRequestCommand>
{
    public async Task Handle(CancelUpgradeRequestCommand request, CancellationToken cancellationToken)
    {
        var upgradeRequest = await upgradeRequestRepository.FindByIdAsync(request.UpgradeRequestId, cancellationToken)
            ?? throw new NotFoundException("Upgrade request not found.");

        var userId = currentUserService.GetCurrentUserId();
        if (upgradeRequest.UserId != userId)
            throw new DomainException("You can only cancel your own upgrade request.");

        if (!upgradeRequest.IsActive)
            throw new DomainException("Upgrade request is no longer active.");

        upgradeRequest.Cancel();
        await upgradeRequestRepository.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new UpgradeRequestCancelledEvent(upgradeRequest.Id, upgradeRequest.UserId, upgradeRequest.AllocationPeriodId),
            cancellationToken);
    }
}
