using Aura.Application.Common.Interfaces;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.AllocationPeriods.Commands.DeleteAllocationPeriod;

public record DeleteAllocationPeriodCommand(Guid Id) : IRequest<Unit>;

public class DeleteAllocationPeriodCommandHandler : IRequestHandler<DeleteAllocationPeriodCommand, Unit>
{
    private readonly IAllocationPeriodRepository _allocationPeriodRepository;

    public DeleteAllocationPeriodCommandHandler(IAllocationPeriodRepository allocationPeriodRepository)
    {
        _allocationPeriodRepository = allocationPeriodRepository;
    }

    public async Task<Unit> Handle(DeleteAllocationPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = await _allocationPeriodRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Allocation period with id '{request.Id}' was not found.");

        if (period.Status != AllocationPeriodStatus.Draft)
            throw new DomainException("Only draft allocation periods can be deleted.");

        _allocationPeriodRepository.Remove(period);
        await _allocationPeriodRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
