using Aura.Domain.Enums;
using Aura.Domain.Events;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Domain.Entities;

public class RoommateGroup
{
    public Guid Id { get; private set; }
    public Guid AllocationPeriodId { get; private set; }
    public Guid DormitoryId { get; private set; }
    public Guid LeaderUserId { get; private set; }
    public RoomSizePreference RoomSizePreference { get; private set; }
    public GroupStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? LockedAt { get; private set; }
    public DateTime? DisbandedAt { get; private set; }
    public DateTime? ExpiredAt { get; private set; }

    private readonly List<GroupMember> _members = [];
    public IReadOnlyCollection<GroupMember> Members => _members.AsReadOnly();

    private readonly List<INotification> _domainEvents = [];
    public IReadOnlyCollection<INotification> DomainEvents => _domainEvents.AsReadOnly();
    public void ClearDomainEvents() => _domainEvents.Clear();

    private RoommateGroup() { }

    public static RoommateGroup Create(
        Guid allocationPeriodId, Guid dormitoryId, Guid leaderUserId, RoomSizePreference roomSizePreference)
    {
        if (allocationPeriodId == Guid.Empty) throw new DomainException("Allocation period is required.");
        if (dormitoryId == Guid.Empty) throw new DomainException("Dormitory is required.");
        if (leaderUserId == Guid.Empty) throw new DomainException("Leader id is required.");
        if (!Enum.IsDefined(roomSizePreference)) throw new DomainException("Invalid room size preference.");

        var now = DateTime.UtcNow;
        var group = new RoommateGroup
        {
            Id = Guid.NewGuid(),
            AllocationPeriodId = allocationPeriodId,
            DormitoryId = dormitoryId,
            LeaderUserId = leaderUserId,
            RoomSizePreference = roomSizePreference,
            Status = GroupStatus.Forming,
            CreatedAt = now,
            ExpiresAt = now.AddHours(48),
        };
        group._members.Add(GroupMember.Create(group.Id, leaderUserId));
        return group;
    }

    public void AddMember(Guid userId)
    {
        if (Status != GroupStatus.Forming)
            throw new DomainException($"Cannot add member: group is {Status}, not Forming.");
        if (userId == Guid.Empty)
            throw new DomainException("User id is required.");
        if (_members.Any(m => m.UserId == userId))
            throw new DomainException("User is already a member of this group.");
        if (_members.Count >= (int)RoomSizePreference)
            throw new DomainException("Group is at capacity.");

        _members.Add(GroupMember.Create(Id, userId));
    }

    public void RemoveMember(Guid userId)
    {
        if (Status != GroupStatus.Forming)
            throw new DomainException($"Cannot remove member: group is {Status}, not Forming.");
        if (userId == LeaderUserId)
            throw new DomainException("The leader cannot leave; disband the group instead.");
        var removed = _members.RemoveAll(m => m.UserId == userId);
        if (removed == 0)
            throw new DomainException("User is not a member of this group.");
    }

    public void ChangeRoomSizePreference(RoomSizePreference newPref)
    {
        if (Status != GroupStatus.Forming)
            throw new DomainException($"Cannot change preference: group is {Status}, not Forming.");
        if (!Enum.IsDefined(newPref)) throw new DomainException("Invalid room size preference.");
        if ((int)newPref < _members.Count)
            throw new DomainException(
                $"Cannot reduce preference below current member count ({_members.Count}). Remove a member first.");

        RoomSizePreference = newPref;
    }

    public void Lock()
    {
        if (Status != GroupStatus.Forming)
            throw new DomainException($"Cannot lock: group is {Status}, not Forming.");
        if (_members.Count != (int)RoomSizePreference)
            throw new DomainException(
                $"Cannot lock: group has {_members.Count} members but preference is {(int)RoomSizePreference}-bed (must be at capacity).");

        Status = GroupStatus.Locked;
        LockedAt = DateTime.UtcNow;

        _domainEvents.Add(new GroupLockedEvent(
            Id, AllocationPeriodId, DormitoryId, LeaderUserId,
            (int)RoomSizePreference,
            _members.Select(m => m.UserId).ToArray()));
    }

    public void Disband()
    {
        if (Status != GroupStatus.Forming)
            throw new DomainException($"Cannot disband: group is {Status}, not Forming.");

        Status = GroupStatus.Disbanded;
        DisbandedAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        if (Status != GroupStatus.Forming)
            throw new DomainException($"Cannot expire: group is {Status}, not Forming.");

        Status = GroupStatus.Expired;
        ExpiredAt = DateTime.UtcNow;

        _domainEvents.Add(new GroupExpiredEvent(
            Id, AllocationPeriodId, DormitoryId,
            _members.Select(m => m.UserId).ToArray()));
    }
}
