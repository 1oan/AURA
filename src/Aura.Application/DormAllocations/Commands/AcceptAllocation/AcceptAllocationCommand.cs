using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.DormAllocations.Commands.AcceptAllocation;

public record AcceptAllocationCommand(Guid AllocationId) : IRequest;

public class AcceptAllocationCommandHandler(
    ICurrentUserService currentUserService,
    IDormAllocationRepository dormAllocationRepository,
    IPublisher publisher) : IRequestHandler<AcceptAllocationCommand>
{
    public async Task Handle(AcceptAllocationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var allocation = await dormAllocationRepository.FindByIdAsync(request.AllocationId, cancellationToken)
            ?? throw new NotFoundException("Allocation not found.");

        if (allocation.UserId != userId)
            throw new DomainException("You can only accept your own allocation.");

        allocation.Accept();
        await dormAllocationRepository.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new AllocationAcceptedEvent(allocation.Id, allocation.UserId, allocation.DormitoryId, allocation.AllocationPeriodId),
            cancellationToken);
    }
}
