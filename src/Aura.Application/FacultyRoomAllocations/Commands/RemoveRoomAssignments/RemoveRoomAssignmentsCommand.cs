using Aura.Application.Common.Interfaces;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.FacultyRoomAllocations.Commands.RemoveRoomAssignments;

public record RemoveRoomAssignmentsCommand(Guid FacultyId, Guid AllocationPeriodId, List<Guid> RoomIds) : IRequest<Unit>;

public class RemoveRoomAssignmentsCommandHandler : IRequestHandler<RemoveRoomAssignmentsCommand, Unit>
{
    private readonly IAllocationPeriodRepository _periodRepository;
    private readonly IFacultyRoomAllocationRepository _allocationRepository;

    public RemoveRoomAssignmentsCommandHandler(
        IAllocationPeriodRepository periodRepository,
        IFacultyRoomAllocationRepository allocationRepository)
    {
        _periodRepository = periodRepository;
        _allocationRepository = allocationRepository;
    }

    public async Task<Unit> Handle(RemoveRoomAssignmentsCommand request, CancellationToken cancellationToken)
    {
        var period = await _periodRepository.FindByIdAsync(request.AllocationPeriodId, cancellationToken)
            ?? throw new NotFoundException("Allocation period not found.");

        if (period.Status == AllocationPeriodStatus.Allocating || period.Status == AllocationPeriodStatus.Closed)
            throw new DomainException("Faculty room allocations are frozen once the period leaves Draft/Open state.");

        var existing = await _allocationRepository.GetByPeriodAndFacultyAsync(
            request.AllocationPeriodId, request.FacultyId, cancellationToken);

        var existingRoomIds = existing.Select(a => a.RoomId).ToHashSet();
        var missingRoomId = request.RoomIds.FirstOrDefault(id => !existingRoomIds.Contains(id));
        if (missingRoomId != default)
            throw new DomainException($"Room {missingRoomId} is not assigned to this faculty for this period.");

        var toRemove = existing.Where(a => request.RoomIds.Contains(a.RoomId)).ToList();
        _allocationRepository.RemoveRange(toRemove);
        await _allocationRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
