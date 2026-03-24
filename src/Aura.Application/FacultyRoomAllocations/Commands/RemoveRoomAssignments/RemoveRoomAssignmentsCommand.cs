using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.FacultyRoomAllocations.Commands.RemoveRoomAssignments;

public record RemoveRoomAssignmentsCommand(Guid FacultyId, Guid AllocationPeriodId, List<Guid> RoomIds) : IRequest<Unit>;

public class RemoveRoomAssignmentsCommandHandler : IRequestHandler<RemoveRoomAssignmentsCommand, Unit>
{
    private readonly IFacultyRoomAllocationRepository _allocationRepository;

    public RemoveRoomAssignmentsCommandHandler(IFacultyRoomAllocationRepository allocationRepository)
    {
        _allocationRepository = allocationRepository;
    }

    public async Task<Unit> Handle(RemoveRoomAssignmentsCommand request, CancellationToken cancellationToken)
    {
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
