using Aura.Application.Common.Interfaces;
using Aura.Application.RoomAssignments.Queries.GetMyRoom;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.RoomAssignments.Queries;

public class GetMyRoomQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();
    private readonly IRoomAssignmentRepository _assignments = Substitute.For<IRoomAssignmentRepository>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _roomId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();

    private GetMyRoomQueryHandler CreateHandler() =>
        new(_currentUser, _periods, _assignments);

    private AllocationPeriod CreatePeriod()
    {
        var period = AllocationPeriod.Create("2025-2026", DateTime.UtcNow, DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(1), 3);
        period.SetPrivateProperty("Id", _periodId);
        return period;
    }

    private RoomAssignment CreateAssignment()
    {
        var dormitory = Dormitory.Create("Dorm Alpha", Guid.NewGuid());
        dormitory.SetPrivateProperty("Id", _dormId);

        var room = Room.Create("201", _dormId, 2, 3, Gender.Male);
        room.SetPrivateProperty("Id", _roomId);
        room.SetPrivateProperty("Dormitory", dormitory);

        var assignment = RoomAssignment.Create(_userId, _roomId, _periodId);
        assignment.SetPrivateProperty("Room", room);
        return assignment;
    }

    [Fact]
    public async Task Handle_NoPeriod_ReturnsNull()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _periods.GetActiveAllocatingAsync(Arg.Any<CancellationToken>())
            .Returns((AllocationPeriod?)null);

        var result = await CreateHandler().Handle(new GetMyRoomQuery(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NoAssignment_ReturnsNull()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _periods.GetActiveAllocatingAsync(Arg.Any<CancellationToken>()).Returns(CreatePeriod());
        _assignments.FindByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns((RoomAssignment?)null);

        var result = await CreateHandler().Handle(new GetMyRoomQuery(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithAssignment_NoRoommates_ReturnsDtoWithEmptyRoommates()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _periods.GetActiveAllocatingAsync(Arg.Any<CancellationToken>()).Returns(CreatePeriod());

        var assignment = CreateAssignment();
        _assignments.FindByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(assignment);
        _assignments.ListRoommatesAsync(_userId, _roomId, _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<RoomAssignment>());

        var result = await CreateHandler().Handle(new GetMyRoomQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.RoomAssignmentId.Should().Be(assignment.Id);
        result.RoomId.Should().Be(_roomId);
        result.RoomNumber.Should().Be("201");
        result.DormitoryName.Should().Be("Dorm Alpha");
        result.Floor.Should().Be(2);
        result.Capacity.Should().Be(3);
        result.Roommates.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithAssignment_HasRoommates_ReturnsDtoWithRoommates()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _periods.GetActiveAllocatingAsync(Arg.Any<CancellationToken>()).Returns(CreatePeriod());

        var assignment = CreateAssignment();
        _assignments.FindByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(assignment);

        var roommateId = Guid.NewGuid();
        var roommateUser = User.Create("roommate@uaic.ro", "hash");
        roommateUser.UpdateProfile("Ana", "Ionescu");
        roommateUser.SetPrivateProperty("Id", roommateId);

        var roommateAssignment = RoomAssignment.Create(roommateId, _roomId, _periodId);
        roommateAssignment.SetPrivateProperty("User", roommateUser);

        _assignments.ListRoommatesAsync(_userId, _roomId, _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<RoomAssignment> { roommateAssignment });

        var result = await CreateHandler().Handle(new GetMyRoomQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Roommates.Should().HaveCount(1);
        result.Roommates[0].UserId.Should().Be(roommateId);
        result.Roommates[0].FirstName.Should().Be("Ana");
        result.Roommates[0].LastName.Should().Be("Ionescu");
    }
}
