using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.AllocationPeriods.Commands.StartAllocating;

public record StartAllocatingCommand(Guid Id) : IRequest<Unit>;

public class StartAllocatingCommandHandler(IAllocationPeriodRepository allocationPeriodRepository)
    : IRequestHandler<StartAllocatingCommand, Unit>
{
    public async Task<Unit> Handle(StartAllocatingCommand request, CancellationToken cancellationToken)
    {
        var period = await allocationPeriodRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Allocation period with id '{request.Id}' was not found.");

        period.StartAllocating();

        await allocationPeriodRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
