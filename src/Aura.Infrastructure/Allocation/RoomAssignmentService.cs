using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;

namespace Aura.Infrastructure.Allocation;

public class RoomAssignmentService(
    IRoomRepository roomRepository,
    IDormAllocationRepository dormAllocationRepository,
    IRoomAssignmentRepository roomAssignmentRepository,
    IUserRepository userRepository,
    IRoommateGroupRepository roommateGroupRepository) : IRoomAssignmentService
{
    public async Task<RoomAssignment> PlaceSoloAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new DomainException("User not found.");

        var allocation = await dormAllocationRepository.FindActiveByUserAsync(userId, cancellationToken);
        if (allocation is null || allocation.Status != AllocationStatus.Accepted)
            throw new DomainException("No Accepted allocation found for this user in an active period.");

        var existing = await roomAssignmentRepository.FindByUserAndPeriodAsync(userId, allocation.AllocationPeriodId, cancellationToken);
        if (existing is not null)
            throw new DomainException("User is already placed in a room for this period.");

        var gender = user.Gender ?? Gender.Male;
        var rooms = await roomRepository.ListByDormitoryAndGenderAsync(allocation.DormitoryId, gender, cancellationToken);
        if (rooms.Count == 0)
            throw new DomainException("No rooms available — contact your dorm admin.");

        var occupancy = await roomAssignmentRepository.GetOccupancyForDormitoryAsync(allocation.DormitoryId, allocation.AllocationPeriodId, cancellationToken);

        var candidate = rooms
            .Select(r => new { Room = r, Occupied = occupancy.TryGetValue(r.Id, out var c) ? c : 0 })
            .Where(x => x.Occupied < x.Room.Capacity)
            .OrderByDescending(x => x.Occupied)
            .ThenBy(x => x.Room.Number)
            .FirstOrDefault();

        if (candidate is null)
            throw new DomainException("No rooms available — contact your dorm admin.");

        var assignment = RoomAssignment.Create(userId, candidate.Room.Id, allocation.AllocationPeriodId);
        await roomAssignmentRepository.AddAsync(assignment, cancellationToken);
        await roomAssignmentRepository.SaveChangesAsync(cancellationToken);

        return assignment;
    }

    public async Task<List<RoomAssignment>> PlaceGroupAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await roommateGroupRepository.FindByIdAsync(groupId, cancellationToken)
            ?? throw new DomainException("Group not found.");

        if (group.Status != GroupStatus.Locked)
            throw new DomainException($"Cannot place group in status {group.Status}.");

        Guid targetRoomId;
        Room targetRoom;
        List<RoomAssignment> currentOccupants;

        if (group.AnchorRoomId is { } anchorId)
        {
            targetRoom = await roomRepository.FindByIdAsync(anchorId, cancellationToken)
                ?? throw new DomainException("Anchor room not found.");
            targetRoomId = anchorId;

            currentOccupants = (await roomAssignmentRepository.ListByRoomAndPeriodAsync(targetRoomId, group.AllocationPeriodId, cancellationToken)).ToList();
            var memberUserIds = group.Members.Select(m => m.UserId).ToHashSet();
            var strangers = currentOccupants.Where(o => !memberUserIds.Contains(o.UserId)).ToList();

            if (strangers.Count > 0)
                throw new DomainException("Anchor room has non-group occupants. Invite them or choose a different room.");
        }
        else
        {
            var leader = await userRepository.FindByIdAsync(group.LeaderUserId, cancellationToken)
                ?? throw new DomainException("Leader user not found.");
            var gender = leader.Gender ?? Gender.Male;
            var rooms = await roomRepository.ListByDormitoryAndGenderAsync(group.DormitoryId, gender, cancellationToken);
            var occupancy = await roomAssignmentRepository.GetOccupancyForDormitoryAsync(group.DormitoryId, group.AllocationPeriodId, cancellationToken);

            var prefSize = (int)group.RoomSizePreference;
            var empty = rooms
                .Where(r => r.Capacity == prefSize && (!occupancy.TryGetValue(r.Id, out var c) || c == 0))
                .OrderBy(r => r.Number)
                .FirstOrDefault();

            if (empty is null)
                throw new DomainException($"No empty room of size {prefSize} available in the dormitory.");

            targetRoom = empty;
            targetRoomId = empty.Id;
            currentOccupants = [];
        }

        var placedUserIds = currentOccupants.Select(o => o.UserId).ToHashSet();
        var unplaced = group.Members.Where(m => !placedUserIds.Contains(m.UserId)).ToList();

        if (currentOccupants.Count + unplaced.Count > targetRoom.Capacity)
            throw new DomainException($"Room capacity {targetRoom.Capacity} cannot fit {currentOccupants.Count} existing + {unplaced.Count} new members.");

        var created = new List<RoomAssignment>();
        foreach (var member in unplaced)
        {
            var existing = await roomAssignmentRepository.FindByUserAndPeriodAsync(member.UserId, group.AllocationPeriodId, cancellationToken);
            if (existing?.RoomId == targetRoomId) continue;
            if (existing is not null)
                throw new DomainException($"Member {member.UserId} is already placed in a different room.");

            var assignment = RoomAssignment.Create(member.UserId, targetRoomId, group.AllocationPeriodId, group.Id);
            await roomAssignmentRepository.AddAsync(assignment, cancellationToken);
            created.Add(assignment);
        }

        await roomAssignmentRepository.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<List<Guid>> ForfeitNonCommittedAsync(Guid allocationPeriodId, CancellationToken cancellationToken)
    {
        var candidates = await dormAllocationRepository.ListAcceptedWithoutRoomAsync(allocationPeriodId, cancellationToken);
        var forfeitedUserIds = new List<Guid>();

        foreach (var allocation in candidates)
        {
            allocation.Forfeit();
            forfeitedUserIds.Add(allocation.UserId);
        }

        if (forfeitedUserIds.Count > 0)
            await dormAllocationRepository.SaveChangesAsync(cancellationToken);

        return forfeitedUserIds;
    }
}
