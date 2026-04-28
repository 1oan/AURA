using Aura.Application.Common.Interfaces;
using Aura.Application.DormPreferences.Commands.SubmitPreferences;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.DormPreferences;

public class SubmitPreferencesCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();
    private readonly IFacultyRoomAllocationRepository _allocations = Substitute.For<IFacultyRoomAllocationRepository>();
    private readonly IDormPreferenceRepository _preferences = Substitute.For<IDormPreferenceRepository>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _facultyId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormA = Guid.NewGuid();
    private readonly Guid _dormB = Guid.NewGuid();
    private readonly Guid _dormC = Guid.NewGuid();

    private SubmitPreferencesCommandHandler CreateHandler() =>
        new(_currentUser, _users, _periods, _allocations, _preferences);

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

    private FacultyRoomAllocation CreateAllocation(Guid dormitoryId, Gender gender)
    {
        var allocation = FacultyRoomAllocation.Create(_facultyId, Guid.NewGuid(), _periodId);
        var room = Room.Create("101", dormitoryId, 1, 3, gender);
        allocation.SetPrivateProperty("Room", room);
        return allocation;
    }

    // ─── Happy path ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithAllDormsRanked_ReplacesPreferences()
    {
        var user = CreateParticipatedUser();
        var period = CreateOpenPeriod();
        var allocations = new List<FacultyRoomAllocation>
        {
            CreateAllocation(_dormA, Gender.Male),
            CreateAllocation(_dormB, Gender.Male),
            CreateAllocation(_dormC, Gender.Male),
        };

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _allocations.GetByPeriodAndFacultyAsync(_periodId, _facultyId, Arg.Any<CancellationToken>())
            .Returns(allocations);

        await CreateHandler().Handle(
            new SubmitPreferencesCommand(_periodId, [_dormA, _dormB, _dormC]),
            CancellationToken.None);

        await _preferences.Received(1).DeleteByUserAndPeriodAsync(
            _userId, _periodId, Arg.Any<CancellationToken>());
        await _preferences.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<DormPreference>>(p => p.Count() == 3),
            Arg.Any<CancellationToken>());
        await _preferences.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSuccess_AssignsRankInSubmittedOrder()
    {
        var user = CreateParticipatedUser();
        var period = CreateOpenPeriod();
        var allocations = new List<FacultyRoomAllocation>
        {
            CreateAllocation(_dormA, Gender.Male),
            CreateAllocation(_dormB, Gender.Male),
        };

        IEnumerable<DormPreference>? captured = null;
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _allocations.GetByPeriodAndFacultyAsync(_periodId, _facultyId, Arg.Any<CancellationToken>())
            .Returns(allocations);
        await _preferences.AddRangeAsync(
            Arg.Do<IEnumerable<DormPreference>>(p => captured = p.ToList()),
            Arg.Any<CancellationToken>());

        await CreateHandler().Handle(
            new SubmitPreferencesCommand(_periodId, [_dormB, _dormA]), // reversed
            CancellationToken.None);

        captured.Should().NotBeNull();
        var list = captured!.ToList();
        list[0].DormitoryId.Should().Be(_dormB);
        list[0].Rank.Should().Be(1);
        list[1].DormitoryId.Should().Be(_dormA);
        list[1].Rank.Should().Be(2);
    }

    [Fact]
    public async Task Handle_IgnoresAllocationsWithDifferentGender()
    {
        var user = CreateParticipatedUser(); // Male
        var period = CreateOpenPeriod();
        var allocations = new List<FacultyRoomAllocation>
        {
            CreateAllocation(_dormA, Gender.Male),
            CreateAllocation(_dormB, Gender.Female), // excluded by gender
        };

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _allocations.GetByPeriodAndFacultyAsync(_periodId, _facultyId, Arg.Any<CancellationToken>())
            .Returns(allocations);

        // Only dormA is available because dormB is Female
        await CreateHandler().Handle(
            new SubmitPreferencesCommand(_periodId, [_dormA]),
            CancellationToken.None);

        await _preferences.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<DormPreference>>(p => p.Count() == 1),
            Arg.Any<CancellationToken>());
    }

    // ─── Guard clauses ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenUserNotFound_ThrowsNotFoundException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = async () => await CreateHandler().Handle(
            new SubmitPreferencesCommand(_periodId, [_dormA]), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("User not found.");
    }

    [Fact]
    public async Task Handle_WhenUserHasNoFaculty_ThrowsDomainException()
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "hash");
        user.SetPrivateProperty("Id", _userId);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        var act = async () => await CreateHandler().Handle(
            new SubmitPreferencesCommand(_periodId, [_dormA]), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*must participate*");
    }

    [Fact]
    public async Task Handle_WhenUserHasNoGender_ThrowsDomainException()
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "hash");
        user.AssignToFaculty(_facultyId);
        // Gender not set
        user.SetPrivateProperty("Id", _userId);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        var act = async () => await CreateHandler().Handle(
            new SubmitPreferencesCommand(_periodId, [_dormA]), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*must participate*");
    }

    [Fact]
    public async Task Handle_WhenPeriodNotFound_ThrowsNotFoundException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateParticipatedUser());
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns((AllocationPeriod?)null);

        var act = async () => await CreateHandler().Handle(
            new SubmitPreferencesCommand(_periodId, [_dormA]), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Allocation period not found.");
    }

    [Fact]
    public async Task Handle_WhenPeriodIsNotOpen_ThrowsDomainException()
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
            new SubmitPreferencesCommand(_periodId, [_dormA]), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*only be submitted while the allocation period is open or allocating*");
    }

    // ─── "All dorms" invariant ───────────────────────────────────────────

    [Fact]
    public async Task Handle_WithIncompleteRanking_ThrowsDomainException()
    {
        var user = CreateParticipatedUser();
        var period = CreateOpenPeriod();
        var allocations = new List<FacultyRoomAllocation>
        {
            CreateAllocation(_dormA, Gender.Male),
            CreateAllocation(_dormB, Gender.Male),
            CreateAllocation(_dormC, Gender.Male),
        };

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _allocations.GetByPeriodAndFacultyAsync(_periodId, _facultyId, Arg.Any<CancellationToken>())
            .Returns(allocations);

        // Only 2 of 3 available dorms submitted
        var act = async () => await CreateHandler().Handle(
            new SubmitPreferencesCommand(_periodId, [_dormA, _dormB]),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*rank all available dormitories*");
    }

    [Fact]
    public async Task Handle_WithExtraDormNotAvailable_ThrowsDomainException()
    {
        var user = CreateParticipatedUser();
        var period = CreateOpenPeriod();
        var allocations = new List<FacultyRoomAllocation>
        {
            CreateAllocation(_dormA, Gender.Male),
        };

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _allocations.GetByPeriodAndFacultyAsync(_periodId, _facultyId, Arg.Any<CancellationToken>())
            .Returns(allocations);

        // Submits dormB which isn't in their available pool
        var act = async () => await CreateHandler().Handle(
            new SubmitPreferencesCommand(_periodId, [_dormA, _dormB]),
            CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*rank all available dormitories*");
    }
}
