using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.AllocationPeriods.Commands.CloseAllocationPeriod;

public record CloseAllocationPeriodCommand(Guid Id) : IRequest<Unit>;

public class CloseAllocationPeriodCommandHandler(
    IAllocationPeriodRepository allocationPeriodRepository,
    IPublisher publisher) : IRequestHandler<CloseAllocationPeriodCommand, Unit>
{
    public async Task<Unit> Handle(CloseAllocationPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = await allocationPeriodRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Allocation period with id '{request.Id}' was not found.");

        period.Close();

        await allocationPeriodRepository.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in period.DomainEvents)
            await publisher.Publish(domainEvent, cancellationToken);
        period.ClearDomainEvents();

        return Unit.Value;
    }
}
