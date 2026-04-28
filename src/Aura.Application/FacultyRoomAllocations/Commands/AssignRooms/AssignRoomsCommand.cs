using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.FacultyRoomAllocations.Commands.AssignRooms;

public record AssignRoomsCommand(Guid FacultyId, Guid AllocationPeriodId, List<Guid> RoomIds) : IRequest<int>;

public class AssignRoomsCommandHandler : IRequestHandler<AssignRoomsCommand, int>
{
    private readonly IFacultyRepository _facultyRepository;
    private readonly IAllocationPeriodRepository _allocationPeriodRepository;
    private readonly IFacultyRoomAllocationRepository _allocationRepository;

    public AssignRoomsCommandHandler(
        IFacultyRepository facultyRepository,
        IAllocationPeriodRepository allocationPeriodRepository,
        IFacultyRoomAllocationRepository allocationRepository)
    {
        _facultyRepository = facultyRepository;
        _allocationPeriodRepository = allocationPeriodRepository;
        _allocationRepository = allocationRepository;
    }

    public async Task<int> Handle(AssignRoomsCommand request, CancellationToken cancellationToken)
    {
        var faculty = await _facultyRepository.FindByIdAsync(request.FacultyId, cancellationToken)
            ?? throw new NotFoundException($"Faculty '{request.FacultyId}' not found.");

        var period = await _allocationPeriodRepository.FindByIdAsync(request.AllocationPeriodId, cancellationToken)
            ?? throw new NotFoundException($"Allocation period '{request.AllocationPeriodId}' not found.");

        if (period.Status == AllocationPeriodStatus.Allocating || period.Status == AllocationPeriodStatus.Closed)
            throw new DomainException("Faculty room allocations are frozen once the period leaves Draft/Open state.");

        foreach (var roomId in request.RoomIds)
        {
            var existing = await _allocationRepository.GetByRoomAndPeriodAsync(roomId, request.AllocationPeriodId, cancellationToken);
            if (existing.Count > 0)
                throw new DomainException($"Room {roomId} is already assigned to a faculty for this period.");
        }

        var allocations = request.RoomIds
            .Select(roomId => FacultyRoomAllocation.Create(request.FacultyId, roomId, request.AllocationPeriodId))
            .ToList();

        await _allocationRepository.AddRangeAsync(allocations, cancellationToken);
        await _allocationRepository.SaveChangesAsync(cancellationToken);

        return allocations.Count;
    }
}
