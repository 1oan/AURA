using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Common;
using MediatR;

namespace Aura.Application.DormAllocations.Queries.GetMyAllocation;

public record GetMyAllocationQuery(Guid AllocationPeriodId) : IRequest<DormAllocationDto?>;

public class GetMyAllocationQueryHandler(
    ICurrentUserService currentUserService,
    IDormAllocationRepository dormAllocationRepository) : IRequestHandler<GetMyAllocationQuery, DormAllocationDto?>
{
    public async Task<DormAllocationDto?> Handle(GetMyAllocationQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var allocation = await dormAllocationRepository.FindActiveByUserAndPeriodAsync(
            userId, request.AllocationPeriodId, cancellationToken);
        if (allocation is null) return null;

        return new DormAllocationDto(
            allocation.Id,
            allocation.UserId,
            allocation.User?.FirstName,
            allocation.User?.LastName,
            null,
            allocation.DormitoryId,
            allocation.Dormitory!.Name,
            allocation.Dormitory.Campus!.Name,
            allocation.AllocationPeriodId,
            allocation.Round,
            allocation.Status.ToString(),
            allocation.AllocatedAt,
            allocation.RespondedAt);
    }
}
