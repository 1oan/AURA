using Aura.Application.Common.Interfaces;
using Aura.Application.FacultyRoomAllocations.Commands.AssignRooms;
using Aura.Application.FacultyRoomAllocations.Commands.RemoveRoomAssignments;
using Aura.Application.FacultyRoomAllocations.Queries.GetFacultyRoomAllocations;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.FacultyRoomAllocations;

public class AssignRoomsCommandHandlerTests
{
    private readonly IFacultyRepository _faculties = Substitute.For<IFacultyRepository>();
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();
    private readonly IFacultyRoomAllocationRepository _allocations = Substitute.For<IFacultyRoomAllocationRepository>();

    private readonly Guid _facultyId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();

    private AssignRoomsCommandHandler Create() => new(_faculties, _periods, _allocations);

    private AllocationPeriod Period()
    {
        var p = AllocationPeriod.Create("2026-2027",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        return p;
    }

    [Fact]
    public async Task Handle_AssignsRooms()
    {
        var faculty = Faculty.Create("Informatica", "INF");
        faculty.SetPrivateProperty("Id", _facultyId);

        _faculties.FindByIdAsync(_facultyId, Arg.Any<CancellationToken>()).Returns(faculty);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(Period());
        _allocations.GetByRoomAndPeriodAsync(Arg.Any<Guid>(), _periodId, Arg.Any<CancellationToken>())
            .Returns([]);

        var count = await Create().Handle(
            new AssignRoomsCommand(_facultyId, _periodId, [Guid.NewGuid(), Guid.NewGuid()]),
            CancellationToken.None);

        count.Should().Be(2);
        await _allocations.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<FacultyRoomAllocation>>(a => a.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenFacultyNotFound_Throws()
    {
        _faculties.FindByIdAsync(_facultyId, Arg.Any<CancellationToken>()).Returns((Faculty?)null);

        var act = async () => await Create().Handle(
            new AssignRoomsCommand(_facultyId, _periodId, [Guid.NewGuid()]),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenPeriodNotFound_Throws()
    {
        var faculty = Faculty.Create("Informatica", "INF");
        _faculties.FindByIdAsync(_facultyId, Arg.Any<CancellationToken>()).Returns(faculty);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns((AllocationPeriod?)null);

        var act = async () => await Create().Handle(
            new AssignRoomsCommand(_facultyId, _periodId, [Guid.NewGuid()]),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenRoomAlreadyAssigned_Throws()
    {
        var faculty = Faculty.Create("Informatica", "INF");
        _faculties.FindByIdAsync(_facultyId, Arg.Any<CancellationToken>()).Returns(faculty);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(Period());
        _allocations.GetByRoomAndPeriodAsync(Arg.Any<Guid>(), _periodId, Arg.Any<CancellationToken>())
            .Returns([FacultyRoomAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), _periodId)]);

        var act = async () => await Create().Handle(
            new AssignRoomsCommand(_facultyId, _periodId, [Guid.NewGuid()]),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class RemoveRoomAssignmentsCommandHandlerTests
{
    private readonly IFacultyRoomAllocationRepository _allocations = Substitute.For<IFacultyRoomAllocationRepository>();

    [Fact]
    public async Task Handle_RemovesExistingAssignments()
    {
        var facultyId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var roomA = Guid.NewGuid();
        var roomB = Guid.NewGuid();
        var allocA = FacultyRoomAllocation.Create(facultyId, roomA, periodId);
        var allocB = FacultyRoomAllocation.Create(facultyId, roomB, periodId);
        _allocations.GetByPeriodAndFacultyAsync(periodId, facultyId, Arg.Any<CancellationToken>())
            .Returns([allocA, allocB]);

        var handler = new RemoveRoomAssignmentsCommandHandler(_allocations);
        await handler.Handle(new RemoveRoomAssignmentsCommand(facultyId, periodId, [roomA]),
            CancellationToken.None);

        _allocations.Received(1).RemoveRange(Arg.Is<IEnumerable<FacultyRoomAllocation>>(
            a => a.Count() == 1 && a.First().RoomId == roomA));
    }

    [Fact]
    public async Task Handle_WhenRoomNotAssigned_Throws()
    {
        var facultyId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        _allocations.GetByPeriodAndFacultyAsync(periodId, facultyId, Arg.Any<CancellationToken>())
            .Returns([]);

        var handler = new RemoveRoomAssignmentsCommandHandler(_allocations);
        var act = async () => await handler.Handle(
            new RemoveRoomAssignmentsCommand(facultyId, periodId, [Guid.NewGuid()]),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}

public class GetFacultyRoomAllocationsQueryHandlerTests
{
    private readonly IFacultyRoomAllocationRepository _allocations = Substitute.For<IFacultyRoomAllocationRepository>();

    [Fact]
    public async Task Handle_ReturnsAllocationsForPeriodAndFaculty()
    {
        var facultyId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var allocation = FacultyRoomAllocation.Create(facultyId, Guid.NewGuid(), periodId);

        var faculty = Faculty.Create("Informatica", "INF");
        allocation.SetPrivateProperty("Faculty", faculty);

        var room = Room.Create("101", Guid.NewGuid(), 1, 3, Gender.Male);
        var dorm = Dormitory.Create("C1", Guid.NewGuid());
        dorm.SetPrivateProperty("Campus", Campus.Create("Codrescu"));
        room.SetPrivateProperty("Dormitory", dorm);
        allocation.SetPrivateProperty("Room", room);

        _allocations.GetByPeriodAndFacultyAsync(periodId, facultyId, Arg.Any<CancellationToken>())
            .Returns([allocation]);

        var handler = new GetFacultyRoomAllocationsQueryHandler(_allocations);
        var result = await handler.Handle(
            new GetFacultyRoomAllocationsQuery(periodId, facultyId), CancellationToken.None);

        result.Should().HaveCount(1);
    }
}
