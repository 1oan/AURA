using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Commands.RunAllocationRound;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.DormAllocations.Commands.AdvanceRound;

public record AdvanceRoundCommand(Guid AllocationPeriodId) : IRequest;

public class AdvanceRoundCommandHandler(
    IAllocationPeriodRepository allocationPeriodRepository,
    IDormAllocationRepository dormAllocationRepository,
    IPublisher publisher,
    ISender sender) : IRequestHandler<AdvanceRoundCommand>
{
    public async Task Handle(AdvanceRoundCommand request, CancellationToken cancellationToken)
    {
        var period = await allocationPeriodRepository.FindByIdAsync(request.AllocationPeriodId, cancellationToken)
            ?? throw new NotFoundException("Allocation period not found.");

        if (period.Status != AllocationPeriodStatus.Allocating)
            throw new DomainException("Period must be in Allocating state to advance a round.");

        var lastRound = await dormAllocationRepository.GetMaxRoundAsync(request.AllocationPeriodId, cancellationToken);
        var nextRound = lastRound + 1;

        // Force-expire any Pending allocations from prior rounds — admin override semantics:
        // "I'm collapsing the response window into this moment."
        var stale = await dormAllocationRepository.GetPendingFromPriorRoundsAsync(
            request.AllocationPeriodId, nextRound, cancellationToken);
        if (stale.Count > 0)
        {
            foreach (var allocation in stale)
            {
                allocation.Expire();
            }
            await dormAllocationRepository.SaveChangesAsync(cancellationToken);

            foreach (var allocation in stale)
            {
                await publisher.Publish(
                    new AllocationExpiredEvent(allocation.Id, allocation.UserId, allocation.DormitoryId, allocation.AllocationPeriodId),
                    cancellationToken);
            }
        }

        await sender.Send(
            new RunAllocationRoundCommand(request.AllocationPeriodId, nextRound),
            cancellationToken);
    }
}
