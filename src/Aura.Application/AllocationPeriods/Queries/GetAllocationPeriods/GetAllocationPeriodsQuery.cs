using Aura.Application.AllocationPeriods.Common;
using Aura.Application.Common.Interfaces;
using MediatR;

namespace Aura.Application.AllocationPeriods.Queries.GetAllocationPeriods;

public record GetAllocationPeriodsQuery : IRequest<List<AllocationPeriodDto>>;

public class GetAllocationPeriodsQueryHandler : IRequestHandler<GetAllocationPeriodsQuery, List<AllocationPeriodDto>>
{
    private readonly IAllocationPeriodRepository _allocationPeriodRepository;

    public GetAllocationPeriodsQueryHandler(IAllocationPeriodRepository allocationPeriodRepository)
    {
        _allocationPeriodRepository = allocationPeriodRepository;
    }

    public async Task<List<AllocationPeriodDto>> Handle(GetAllocationPeriodsQuery request, CancellationToken cancellationToken)
    {
        var periods = await _allocationPeriodRepository.GetAllAsync(cancellationToken);

        return periods
            .Select(p => new AllocationPeriodDto(p.Id, p.Name, p.StartDate, p.EndDate, p.Status.ToString()))
            .ToList();
    }
}
