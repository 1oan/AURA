using Aura.Application.Common.Interfaces;
using Aura.Application.DormPreferences.Queries.GetAvailableDormitories;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.DormPreferences;

public class GetAvailableDormitoriesQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();
    private readonly IFacultyRoomAllocationRepository _allocations = Substitute.For<IFacultyRoomAllocationRepository>();
    private readonly IDormAllocationRepository _dormAllocations = Substitute.For<IDormAllocationRepository>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _facultyId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();

    private GetAvailableDormitoriesQueryHandler CreateHandler() =>
        new(_currentUser, _users, _periods, _allocations, _dormAllocations);

    private User CreateParticipatedUser(Gender gender = Gender.Male)
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "hash");
        user.UpdateProfile("Ioan", "Virlescu");
        user.AssignToFaculty(_facultyId);
        user.SetGender(gender);
        user.SetPrivateProperty("Id", _userId);
        return user;
    }

    private AllocationPeriod CreateOpenPeriod()
    {
        var period = AllocationPeriod.Create(
            "2026-2027",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            3);
        period.Activate();
        return period;
    }

    private FacultyRoomAllocation CreateAllocation(
        string roomNumber, Guid dormitoryId, string dormName, Guid campusId, string campusName, int capacity, Gender gender)
    {
        var campus = Campus.Create(campusName);
        campus.SetPrivateProperty("Id", campusId);

        var dormitory = Dormitory.Create(dormName, campusId);
        dormitory.SetPrivateProperty("Id", dormitoryId);
        dormitory.SetPrivateProperty("Campus", campus);

        var room = Room.Create(roomNumber, dormitoryId, 1, capacity, gender);
        room.SetPrivateProperty("Dormitory", dormitory);

        var allocation = FacultyRoomAllocation.Create(_facultyId, Guid.NewGuid(), _periodId);
        allocation.SetPrivateProperty("Room", room);
        return allocation;
    }

    // ─── Happy path ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_GroupsRoomsByDormitoryAndSumsCapacity()
    {
        var user = CreateParticipatedUser();
        var period = CreateOpenPeriod();

        var dormA = Guid.NewGuid();
        var campusA = Guid.NewGuid();
        var allocations = new List<FacultyRoomAllocation>
        {
            CreateAllocation("101", dormA, "C1", campusA, "Codrescu", 3, Gender.Male),
            CreateAllocation("102", dormA, "C1", campusA, "Codrescu", 2, Gender.Male),
            CreateAllocation("103", dormA, "C1", campusA, "Codrescu", 4, Gender.Male),
        };

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _allocations.GetByPeriodAndFacultyAsync(_periodId, _facultyId, Arg.Any<CancellationToken>())
            .Returns(allocations);

        var result = await CreateHandler().Handle(
            new GetAvailableDormitoriesQuery(_periodId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].DormitoryId.Should().Be(dormA);
        result[0].DormitoryName.Should().Be("C1");
        result[0].CampusName.Should().Be("Codrescu");
        result[0].AvailableSpots.Should().Be(9); // 3 + 2 + 4
    }

    [Fact]
    public async Task Handle_FiltersOutRoomsOfOtherGender()
    {
        var user = CreateParticipatedUser(Gender.Male);
        var period = CreateOpenPeriod();

        var dormA = Guid.NewGuid();
        var dormB = Guid.NewGuid();
        var campusA = Guid.NewGuid();
        var allocations = new List<FacultyRoomAllocation>
        {
            CreateAllocation("101", dormA, "C1", campusA, "Codrescu", 3, Gender.Male),
            CreateAllocation("102", dormB, "C2", campusA, "Codrescu", 3, Gender.Female),
        };

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _allocations.GetByPeriodAndFacultyAsync(_periodId, _facultyId, Arg.Any<CancellationToken>())
            .Returns(allocations);

        var result = await CreateHandler().Handle(
            new GetAvailableDormitoriesQuery(_periodId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].DormitoryId.Should().Be(dormA);
    }

    [Fact]
    public async Task Handle_SortsByCampusThenDormitoryName()
    {
        var user = CreateParticipatedUser();
        var period = CreateOpenPeriod();

        var dormA = Guid.NewGuid();
        var dormB = Guid.NewGuid();
        var dormC = Guid.NewGuid();
        var campusX = Guid.NewGuid();
        var campusY = Guid.NewGuid();
        var allocations = new List<FacultyRoomAllocation>
        {
            CreateAllocation("101", dormB, "B-Dorm", campusY, "Y-Campus", 3, Gender.Male),
            CreateAllocation("102", dormC, "A-Dorm", campusX, "X-Campus", 3, Gender.Male),
            CreateAllocation("103", dormA, "Z-Dorm", campusX, "X-Campus", 3, Gender.Male),
        };

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _allocations.GetByPeriodAndFacultyAsync(_periodId, _facultyId, Arg.Any<CancellationToken>())
            .Returns(allocations);

        var result = await CreateHandler().Handle(
            new GetAvailableDormitoriesQuery(_periodId), CancellationToken.None);

        result.Should().HaveCount(3);
        result[0].CampusName.Should().Be("X-Campus");
        result[0].DormitoryName.Should().Be("A-Dorm");
        result[1].CampusName.Should().Be("X-Campus");
        result[1].DormitoryName.Should().Be("Z-Dorm");
        result[2].CampusName.Should().Be("Y-Campus");
    }

    [Fact]
    public async Task Handle_WithNoMatchingAllocations_ReturnsEmptyList()
    {
        var user = CreateParticipatedUser();
        var period = CreateOpenPeriod();

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _allocations.GetByPeriodAndFacultyAsync(_periodId, _facultyId, Arg.Any<CancellationToken>())
            .Returns([]);

        var result = await CreateHandler().Handle(
            new GetAvailableDormitoriesQuery(_periodId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    // ─── Guard clauses ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = async () => await CreateHandler().Handle(
            new GetAvailableDormitoriesQuery(_periodId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenUserHasNoFacultyOrGender_ThrowsDomainException()
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "hash");
        user.SetPrivateProperty("Id", _userId);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        var act = async () => await CreateHandler().Handle(
            new GetAvailableDormitoriesQuery(_periodId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_WhenPeriodNotFound_ThrowsNotFoundException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateParticipatedUser());
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns((AllocationPeriod?)null);

        var act = async () => await CreateHandler().Handle(
            new GetAvailableDormitoriesQuery(_periodId), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenPeriodNotOpen_ThrowsDomainException()
    {
        var period = AllocationPeriod.Create("2026-2027",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            3);
        // Still Draft

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateParticipatedUser());
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);

        var act = async () => await CreateHandler().Handle(
            new GetAvailableDormitoriesQuery(_periodId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*not accepting preference submissions*");
    }

    [Fact]
    public async Task Handle_WhenStudentTerminalInPeriod_Throws()
    {
        var period = AllocationPeriod.Create("2026-2027",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            3);
        period.Activate();
        period.StartAllocating();

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateParticipatedUser());
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _dormAllocations.HasTerminalForUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(true);

        var act = async () => await CreateHandler().Handle(
            new GetAvailableDormitoriesQuery(_periodId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*no longer eligible*");
    }
}
