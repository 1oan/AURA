using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Events;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class RoommateGroupTests
{
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _leaderId = Guid.NewGuid();

    private RoommateGroup CreateThreeBed() =>
        RoommateGroup.Create(_periodId, _dormId, _leaderId, RoomSizePreference.ThreeBed);

    [Fact]
    public void Create_ValidArgs_ReturnsFormingGroupWithLeaderAsFirstMember()
    {
        var group = CreateThreeBed();

        group.AllocationPeriodId.Should().Be(_periodId);
        group.DormitoryId.Should().Be(_dormId);
        group.LeaderUserId.Should().Be(_leaderId);
        group.RoomSizePreference.Should().Be(RoomSizePreference.ThreeBed);
        group.Status.Should().Be(GroupStatus.Forming);
        group.Members.Should().ContainSingle().Which.UserId.Should().Be(_leaderId);
        group.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(48), TimeSpan.FromSeconds(2));
        group.LockedAt.Should().BeNull();
        group.DisbandedAt.Should().BeNull();
        group.ExpiredAt.Should().BeNull();
    }

    [Fact]
    public void Create_EmptyLeaderId_Throws()
    {
        var act = () => RoommateGroup.Create(_periodId, _dormId, Guid.Empty, RoomSizePreference.TwoBed);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddMember_BelowCapacity_AddsMember()
    {
        var group = CreateThreeBed();
        var newMember = Guid.NewGuid();
        group.AddMember(newMember);
        group.Members.Should().HaveCount(2);
        group.Members.Should().Contain(m => m.UserId == newMember);
    }

    [Fact]
    public void AddMember_AtCapacity_Throws()
    {
        var group = RoommateGroup.Create(_periodId, _dormId, _leaderId, RoomSizePreference.TwoBed);
        group.AddMember(Guid.NewGuid());
        var act = () => group.AddMember(Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*capacity*");
    }

    [Fact]
    public void AddMember_NotForming_Throws()
    {
        var group = CreateThreeBed();
        group.AddMember(Guid.NewGuid());
        group.AddMember(Guid.NewGuid());
        group.Lock();
        var act = () => group.AddMember(Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*Forming*");
    }

    [Fact]
    public void AddMember_DuplicateUser_Throws()
    {
        var group = CreateThreeBed();
        var memberId = Guid.NewGuid();
        group.AddMember(memberId);
        var act = () => group.AddMember(memberId);
        act.Should().Throw<DomainException>().WithMessage("*already*");
    }

    [Fact]
    public void RemoveMember_NonLeader_Removes()
    {
        var group = CreateThreeBed();
        var memberId = Guid.NewGuid();
        group.AddMember(memberId);
        group.RemoveMember(memberId);
        group.Members.Should().HaveCount(1);
        group.Members.Should().NotContain(m => m.UserId == memberId);
    }

    [Fact]
    public void RemoveMember_Leader_Throws()
    {
        var group = CreateThreeBed();
        var act = () => group.RemoveMember(_leaderId);
        act.Should().Throw<DomainException>().WithMessage("*leader*");
    }

    [Fact]
    public void RemoveMember_NotForming_Throws()
    {
        var group = CreateThreeBed();
        var memberId = Guid.NewGuid();
        group.AddMember(memberId);
        group.AddMember(Guid.NewGuid());
        group.Lock();

        var act = () => group.RemoveMember(memberId);

        act.Should().Throw<DomainException>().WithMessage("*Forming*");
    }

    [Fact]
    public void ChangeRoomSizePreference_ValidNewPref_Updates()
    {
        var group = CreateThreeBed();
        group.ChangeRoomSizePreference(RoomSizePreference.TwoBed);
        group.RoomSizePreference.Should().Be(RoomSizePreference.TwoBed);
    }

    [Fact]
    public void ChangeRoomSizePreference_BelowMemberCount_Throws()
    {
        var group = CreateThreeBed();
        group.AddMember(Guid.NewGuid());
        group.AddMember(Guid.NewGuid());
        var act = () => group.ChangeRoomSizePreference(RoomSizePreference.TwoBed);
        act.Should().Throw<DomainException>().WithMessage("*member*");
    }

    [Fact]
    public void Lock_AtCapacity_TransitionsAndRaisesEvent()
    {
        var group = CreateThreeBed();
        group.AddMember(Guid.NewGuid());
        group.AddMember(Guid.NewGuid());
        group.Lock();
        group.Status.Should().Be(GroupStatus.Locked);
        group.LockedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        group.DomainEvents.Should().ContainSingle(e => e is GroupLockedEvent);
    }

    [Fact]
    public void Lock_BelowCapacity_Throws()
    {
        var group = CreateThreeBed();
        var act = () => group.Lock();
        act.Should().Throw<DomainException>().WithMessage("*capacity*");
    }

    [Fact]
    public void Lock_AlreadyLocked_Throws()
    {
        var group = CreateThreeBed();
        group.AddMember(Guid.NewGuid());
        group.AddMember(Guid.NewGuid());
        group.Lock();
        var act = () => group.Lock();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Disband_FromForming_Transitions()
    {
        var group = CreateThreeBed();
        group.Disband();
        group.Status.Should().Be(GroupStatus.Disbanded);
        group.DisbandedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Disband_FromLocked_Throws()
    {
        var group = CreateThreeBed();
        group.AddMember(Guid.NewGuid());
        group.AddMember(Guid.NewGuid());
        group.Lock();
        var act = () => group.Disband();
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Expire_FromForming_TransitionsAndRaisesEvent()
    {
        var group = CreateThreeBed();
        group.Expire();
        group.Status.Should().Be(GroupStatus.Expired);
        group.ExpiredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        group.DomainEvents.Should().ContainSingle(e => e is GroupExpiredEvent);
    }
}
