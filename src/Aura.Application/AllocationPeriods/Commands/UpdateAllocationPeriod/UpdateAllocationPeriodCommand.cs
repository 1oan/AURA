using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.AllocationPeriods.Commands.UpdateAllocationPeriod;

public record UpdateAllocationPeriodCommand(Guid Id, string Name, DateTime StartDate, DateTime EndDate) : IRequest<Unit>;

public class UpdateAllocationPeriodCommandHandler : IRequestHandler<UpdateAllocationPeriodCommand, Unit>
{
    private readonly IAllocationPeriodRepository _allocationPeriodRepository;

    public UpdateAllocationPeriodCommandHandler(IAllocationPeriodRepository allocationPeriodRepository)
    {
        _allocationPeriodRepository = allocationPeriodRepository;
    }

    public async Task<Unit> Handle(UpdateAllocationPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = await _allocationPeriodRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Allocation period with id '{request.Id}' was not found.");

        period.Update(request.Name, request.StartDate, request.EndDate);

        await _allocationPeriodRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
