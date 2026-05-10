using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.DormAllocations.Commands.DeclineAllocation;

public record DeclineAllocationCommand(Guid AllocationId) : IRequest;

public class DeclineAllocationCommandHandler(
    ICurrentUserService currentUserService,
    IDormAllocationRepository dormAllocationRepository,
    IPublisher publisher) : IRequestHandler<DeclineAllocationCommand>
{
    public async Task Handle(DeclineAllocationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var allocation = await dormAllocationRepository.FindByIdAsync(request.AllocationId, cancellationToken)
            ?? throw new NotFoundException("Allocation not found.");

        if (allocation.UserId != userId)
            throw new DomainException("You can only decline your own allocation.");

        allocation.Decline();
        await dormAllocationRepository.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new AllocationDeclinedEvent(allocation.Id, allocation.UserId, allocation.DormitoryId, allocation.AllocationPeriodId),
            cancellationToken);
    }
}
