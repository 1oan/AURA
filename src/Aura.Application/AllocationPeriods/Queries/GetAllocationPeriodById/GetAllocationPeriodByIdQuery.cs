using Aura.Application.AllocationPeriods.Common;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.AllocationPeriods.Queries.GetAllocationPeriodById;

public record GetAllocationPeriodByIdQuery(Guid Id) : IRequest<AllocationPeriodDto>;

public class GetAllocationPeriodByIdQueryHandler : IRequestHandler<GetAllocationPeriodByIdQuery, AllocationPeriodDto>
{
    private readonly IAllocationPeriodRepository _allocationPeriodRepository;

    public GetAllocationPeriodByIdQueryHandler(IAllocationPeriodRepository allocationPeriodRepository)
    {
        _allocationPeriodRepository = allocationPeriodRepository;
    }

    public async Task<AllocationPeriodDto> Handle(GetAllocationPeriodByIdQuery request, CancellationToken cancellationToken)
    {
        var period = await _allocationPeriodRepository.FindByIdAsync(request.Id, cancellationToken)
            ?? throw new DomainException($"Allocation period with id '{request.Id}' was not found.");

        return new AllocationPeriodDto(period.Id, period.Name, period.StartDate, period.EndDate, period.Status.ToString());
    }
}
