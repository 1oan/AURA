using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.AllocationPeriods.Commands.CloseAllocationPeriod;

public record CloseAllocationPeriodCommand(Guid Id) : IRequest<Unit>;

public class CloseAllocationPeriodCommandHandler : IRequestHandler<CloseAllocationPeriodCommand, Unit>
{
    private readonly IAllocationPeriodRepository _allocationPeriodRepository;

    public CloseAllocationPeriodCommandHandler(IAllocationPeriodRepository allocationPeriodRepository)
    {
        _allocationPeriodRepository = allocationPeriodRepository;
    }

    public async Task<Unit> Handle(CloseAllocationPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = await _allocationPeriodRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Allocation period with id '{request.Id}' was not found.");

        period.Close();

        await _allocationPeriodRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
