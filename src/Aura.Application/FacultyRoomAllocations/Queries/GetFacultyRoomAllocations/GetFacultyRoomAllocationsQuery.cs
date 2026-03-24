using Aura.Application.Common.Interfaces;
using Aura.Application.FacultyRoomAllocations.Common;
using MediatR;

namespace Aura.Application.FacultyRoomAllocations.Queries.GetFacultyRoomAllocations;

public record GetFacultyRoomAllocationsQuery(Guid AllocationPeriodId, Guid? FacultyId) : IRequest<List<FacultyRoomAllocationDto>>;

public class GetFacultyRoomAllocationsQueryHandler : IRequestHandler<GetFacultyRoomAllocationsQuery, List<FacultyRoomAllocationDto>>
{
    private readonly IFacultyRoomAllocationRepository _allocationRepository;

    public GetFacultyRoomAllocationsQueryHandler(IFacultyRoomAllocationRepository allocationRepository)
    {
        _allocationRepository = allocationRepository;
    }

    public async Task<List<FacultyRoomAllocationDto>> Handle(GetFacultyRoomAllocationsQuery request, CancellationToken cancellationToken)
    {
        var allocations = await _allocationRepository.GetByPeriodAndFacultyAsync(
            request.AllocationPeriodId, request.FacultyId, cancellationToken);

        return allocations
            .Select(a => new FacultyRoomAllocationDto(a.Id, a.FacultyId, a.RoomId, a.AllocationPeriodId))
            .ToList();
    }
}
