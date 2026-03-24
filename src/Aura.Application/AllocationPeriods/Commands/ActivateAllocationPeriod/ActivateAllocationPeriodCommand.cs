using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.AllocationPeriods.Commands.ActivateAllocationPeriod;

public record ActivateAllocationPeriodCommand(Guid Id) : IRequest<Unit>;

public class ActivateAllocationPeriodCommandHandler : IRequestHandler<ActivateAllocationPeriodCommand, Unit>
{
    private readonly IAllocationPeriodRepository _allocationPeriodRepository;

    public ActivateAllocationPeriodCommandHandler(IAllocationPeriodRepository allocationPeriodRepository)
    {
        _allocationPeriodRepository = allocationPeriodRepository;
    }

    public async Task<Unit> Handle(ActivateAllocationPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = await _allocationPeriodRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Allocation period with id '{request.Id}' was not found.");

        if (await _allocationPeriodRepository.AnyOpenAsync(cancellationToken))
            throw new DomainException("Only one allocation period can be open at a time.");

        period.Activate();

        await _allocationPeriodRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
