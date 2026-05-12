using Aura.Application.Common.Interfaces;
using MediatR;

namespace Aura.Application.RoomAssignments.Queries.GetMyRoom;

public record RoommateDto(Guid UserId, string FirstName, string LastName);

public record RoomDto(
    Guid RoomAssignmentId,
    Guid RoomId,
    string RoomNumber,
    string DormitoryName,
    int Floor,
    int Capacity,
    DateTime AssignedAt,
    IReadOnlyList<RoommateDto> Roommates);

public record GetMyRoomQuery : IRequest<RoomDto?>;

public class GetMyRoomQueryHandler(
    ICurrentUserService currentUserService,
    IAllocationPeriodRepository periodRepository,
    IRoomAssignmentRepository roomAssignmentRepository) : IRequestHandler<GetMyRoomQuery, RoomDto?>
{
    public async Task<RoomDto?> Handle(GetMyRoomQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();

        var period = await periodRepository.GetActiveAllocatingAsync(cancellationToken);
        if (period is null) return null;

        var assignment = await roomAssignmentRepository.FindByUserAndPeriodAsync(userId, period.Id, cancellationToken);
        if (assignment is null) return null;

        var roommates = await roomAssignmentRepository.ListRoommatesAsync(userId, assignment.RoomId, period.Id, cancellationToken);

        return new RoomDto(
            assignment.Id,
            assignment.RoomId,
            assignment.Room!.Number,
            assignment.Room.Dormitory!.Name,
            assignment.Room.Floor,
            assignment.Room.Capacity,
            assignment.AssignedAt,
            roommates.Select(r => new RoommateDto(r.UserId, r.User!.FirstName, r.User.LastName)).ToList());
    }
}
