using Aura.Application.AllocationPeriods.Common;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using MediatR;

namespace Aura.Application.AllocationPeriods.Commands.CreateAllocationPeriod;

public record CreateAllocationPeriodCommand(string Name, DateTime StartDate, DateTime EndDate, DateTime Round1Date, int ResponseWindowDays) : IRequest<AllocationPeriodDto>;

public class CreateAllocationPeriodCommandHandler : IRequestHandler<CreateAllocationPeriodCommand, AllocationPeriodDto>
{
    private readonly IAllocationPeriodRepository _allocationPeriodRepository;

    public CreateAllocationPeriodCommandHandler(IAllocationPeriodRepository allocationPeriodRepository)
    {
        _allocationPeriodRepository = allocationPeriodRepository;
    }

    public async Task<AllocationPeriodDto> Handle(CreateAllocationPeriodCommand request, CancellationToken cancellationToken)
    {
        var period = AllocationPeriod.Create(request.Name, request.StartDate, request.EndDate, request.Round1Date, request.ResponseWindowDays);

        await _allocationPeriodRepository.AddAsync(period, cancellationToken);
        await _allocationPeriodRepository.SaveChangesAsync(cancellationToken);

        return new AllocationPeriodDto(period.Id, period.Name, period.StartDate, period.EndDate, period.Status.ToString());
    }
}
