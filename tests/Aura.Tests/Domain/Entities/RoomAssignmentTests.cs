using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class RoomAssignmentTests
{
    [Fact]
    public void Create_WithValidInputs_Succeeds()
    {
        var userId = Guid.NewGuid();
        var roomId = Guid.NewGuid();
        var periodId = Guid.NewGuid();

        var assignment = RoomAssignment.Create(userId, roomId, periodId);

        assignment.Id.Should().NotBe(Guid.Empty);
        assignment.UserId.Should().Be(userId);
        assignment.RoomId.Should().Be(roomId);
        assignment.AllocationPeriodId.Should().Be(periodId);
        assignment.RoommateGroupId.Should().BeNull();
        assignment.AssignedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Create_WithGroupId_StoresGroupId()
    {
        var groupId = Guid.NewGuid();
        var assignment = RoomAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), groupId);
        assignment.RoommateGroupId.Should().Be(groupId);
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws()
    {
        var act = () => RoomAssignment.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*User ID is required*");
    }

    [Fact]
    public void Create_WithEmptyRoomId_Throws()
    {
        var act = () => RoomAssignment.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());
        act.Should().Throw<DomainException>().WithMessage("*Room ID is required*");
    }

    [Fact]
    public void Create_WithEmptyPeriodId_Throws()
    {
        var act = () => RoomAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);
        act.Should().Throw<DomainException>().WithMessage("*Allocation period ID is required*");
    }
}
