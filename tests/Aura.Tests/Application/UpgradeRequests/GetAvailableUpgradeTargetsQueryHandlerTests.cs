using Aura.Application.Common.Interfaces;
using Aura.Application.UpgradeRequests.Queries.GetAvailableUpgradeTargets;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.UpgradeRequests;

public class GetAvailableUpgradeTargetsQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();
    private readonly IFacultyRoomAllocationRepository _facultyRoomAllocations = Substitute.For<IFacultyRoomAllocationRepository>();
    private readonly IDormAllocationRepository _dormAllocations = Substitute.For<IDormAllocationRepository>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _facultyId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _currentDormId = Guid.NewGuid();
    private readonly Guid _dormA = Guid.NewGuid();
    private readonly Guid _dormB = Guid.NewGuid();
    private readonly Guid _campusId = Guid.NewGuid();

    private GetAvailableUpgradeTargetsQueryHandler CreateHandler() =>
        new(_currentUser, _users, _periods, _facultyRoomAllocations, _dormAllocations);

    private User CreateParticipatedUser()
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "hash");
        user.UpdateProfile("Ioan", "Virlescu");
        user.AssignToFaculty(_facultyId);
        user.SetGender(Gender.Male);
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

    private DormAllocation CreateAcceptedAllocation()
    {
        var allocation = DormAllocation.Create(_userId, _currentDormId, _periodId, 1);
        allocation.Accept();
        return allocation;
    }

    private FacultyRoomAllocation CreateAllocation(Guid dormitoryId, string dormName, string campusName, Gender gender)
    {
        var campus = Campus.Create(campusName);
        campus.SetPrivateProperty("Id", _campusId);

        var dormitory = Dormitory.Create(dormName, _campusId);
        dormitory.SetPrivateProperty("Id", dormitoryId);
        dormitory.SetPrivateProperty("Campus", campus);

        var room = Room.Create("101", dormitoryId, 1, 3, gender);
        room.SetPrivateProperty("Dormitory", dormitory);

        var allocation = FacultyRoomAllocation.Create(_facultyId, Guid.NewGuid(), _periodId);
        allocation.SetPrivateProperty("Room", room);
        return allocation;
    }

    [Fact]
    public async Task Handle_UserNotParticipating_ThrowsDomainException()
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "hash");
        user.SetPrivateProperty("Id", _userId);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        var act = async () => await CreateHandler().Handle(
            new GetAvailableUpgradeTargetsQuery(_periodId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*must participate*");
    }

    [Fact]
    public async Task Handle_NoActiveAllocation_ThrowsDomainException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateParticipatedUser());
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(CreateOpenPeriod());
        _dormAllocations.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);

        var act = async () => await CreateHandler().Handle(
            new GetAvailableUpgradeTargetsQuery(_periodId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*active allocation*");
    }

    [Fact]
    public async Task Handle_HappyPath_ReturnsDormsExcludingCurrent()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateParticipatedUser());
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(CreateOpenPeriod());
        _dormAllocations.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(CreateAcceptedAllocation());

        _facultyRoomAllocations.GetByPeriodAndFacultyAsync(_periodId, _facultyId, Arg.Any<CancellationToken>())
            .Returns(new List<FacultyRoomAllocation>
            {
                CreateAllocation(_currentDormId, "Current", "X-Campus", Gender.Male),
                CreateAllocation(_dormA, "A-Dorm", "X-Campus", Gender.Male),
                CreateAllocation(_dormB, "B-Dorm", "X-Campus", Gender.Male),
            });

        var result = await CreateHandler().Handle(
            new GetAvailableUpgradeTargetsQuery(_periodId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(d => d.DormitoryId).Should().NotContain(_currentDormId);
        result.Select(d => d.DormitoryId).Should().BeEquivalentTo(new[] { _dormA, _dormB });
    }

    [Fact]
    public async Task Handle_OnlyCurrentDormAvailable_ReturnsEmptyList()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateParticipatedUser());
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(CreateOpenPeriod());
        _dormAllocations.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(CreateAcceptedAllocation());

        _facultyRoomAllocations.GetByPeriodAndFacultyAsync(_periodId, _facultyId, Arg.Any<CancellationToken>())
            .Returns(new List<FacultyRoomAllocation>
            {
                CreateAllocation(_currentDormId, "Current", "X-Campus", Gender.Male),
            });

        var result = await CreateHandler().Handle(
            new GetAvailableUpgradeTargetsQuery(_periodId), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
