using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Commands.RunAllocationRound;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using MediatR;
using NSubstitute;

namespace Aura.Tests.Application.DormAllocations;

public class RunAllocationRoundCommandHandlerTests
{
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();
    private readonly IDormAllocationRepository _allocations = Substitute.For<IDormAllocationRepository>();
    private readonly IDormPreferenceRepository _preferences = Substitute.For<IDormPreferenceRepository>();
    private readonly IStudentRecordRepository _records = Substitute.For<IStudentRecordRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private readonly Guid _periodId = Guid.NewGuid();

    private RunAllocationRoundCommandHandler Create() =>
        new(_periods, _allocations, _preferences, _records, _users, _publisher);

    private AllocationPeriod AllocatingPeriod()
    {
        var period = AllocationPeriod.Create(
            "test",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            responseWindowDays: 3);
        period.SetPrivateProperty("Id", _periodId);
        period.Activate();
        period.StartAllocating();
        return period;
    }

    private (User user, StudentRecord record, DormPreference pref) CreateStudent(
        Guid userId, Guid facultyId, Gender gender, int points, string matriculationCode,
        Guid dormId, int rank, DateTime? submissionTime = null)
    {
        var user = User.Create($"{matriculationCode}@uaic.ro", "h");
        user.SetPrivateProperty("Id", userId);
        user.AssignToFaculty(facultyId);
        user.SetGender(gender);

        var record = StudentRecord.Create(matriculationCode, "First", "Last", points, gender, facultyId, _periodId);
        record.LinkToUser(userId);

        var pref = DormPreference.Create(userId, _periodId, dormId, rank);
        if (submissionTime.HasValue)
            pref.SetPrivateProperty("CreatedAt", submissionTime.Value);

        return (user, record, pref);
    }

    // ─── Guard clauses ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_PeriodNotAllocating_Throws()
    {
        var period = AllocationPeriod.Create("t",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc), 3);
        period.SetPrivateProperty("Id", _periodId);
        // Status remains Draft

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);

        var act = async () => await Create().Handle(
            new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Allocating*");
    }

    // ─── Empty pool / early exits ────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyPool_ProducesZeroAllocations()
    {
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>()).Returns(new List<StudentRecord>());

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<AllocationCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    // ─── Happy path ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SingleStudentSinglePreference_Allocates()
    {
        var dormId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (user, record, pref) = CreateStudent(userId, facultyId, Gender.Male, 9, "M001", dormId, 1);

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>()).Returns(new List<StudentRecord> { record });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<User> { user });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref });
        _allocations.FindActiveByUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);
        _allocations.HasTerminalForUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(false);
        _allocations.GetAvailableCapacityAsync(dormId, facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(1);

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        await _allocations.Received(1).AddAsync(
            Arg.Is<DormAllocation>(a => a.UserId == userId && a.DormitoryId == dormId && a.Round == 1),
            Arg.Any<CancellationToken>());
        await _allocations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<AllocationCreatedEvent>(e => e.UserId == userId && e.DormitoryId == dormId),
            Arg.Any<CancellationToken>());
    }

    // ─── Priority / tie-break rules ──────────────────────────────────────

    [Fact]
    public async Task Handle_TwoStudentsCompetingForLastBed_HigherPointsWins()
    {
        var dormId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var highId = Guid.NewGuid();
        var lowId = Guid.NewGuid();

        var (highUser, highRecord, highPref) = CreateStudent(highId, facultyId, Gender.Male, 20, "M002", dormId, 1);
        var (lowUser, lowRecord, lowPref) = CreateStudent(lowId, facultyId, Gender.Male, 5, "M001", dormId, 1);

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>())
            .Returns(new List<StudentRecord> { highRecord, lowRecord });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { highUser, lowUser });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { highPref, lowPref });

        _allocations.FindActiveByUserAndPeriodAsync(Arg.Any<Guid>(), _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);
        _allocations.HasTerminalForUserAndPeriodAsync(Arg.Any<Guid>(), _periodId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Only 1 spot; return 1 for the first call and 0 for subsequent calls
        _allocations.GetAvailableCapacityAsync(dormId, facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(1, 0);

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        // Only higher-points student allocated
        await _allocations.Received(1).AddAsync(
            Arg.Is<DormAllocation>(a => a.UserId == highId),
            Arg.Any<CancellationToken>());
        await _allocations.DidNotReceive().AddAsync(
            Arg.Is<DormAllocation>(a => a.UserId == lowId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TiedPoints_EarlierSubmissionWins()
    {
        var dormId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var earlyId = Guid.NewGuid();
        var lateId = Guid.NewGuid();

        var earlyTime = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);
        var lateTime = new DateTime(2026, 9, 1, 11, 0, 0, DateTimeKind.Utc);

        var (earlyUser, earlyRecord, earlyPref) =
            CreateStudent(earlyId, facultyId, Gender.Male, 10, "M002", dormId, 1, earlyTime);
        var (lateUser, lateRecord, latePref) =
            CreateStudent(lateId, facultyId, Gender.Male, 10, "M001", dormId, 1, lateTime);

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>())
            .Returns(new List<StudentRecord> { earlyRecord, lateRecord });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { earlyUser, lateUser });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { earlyPref, latePref });

        _allocations.FindActiveByUserAndPeriodAsync(Arg.Any<Guid>(), _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);
        _allocations.HasTerminalForUserAndPeriodAsync(Arg.Any<Guid>(), _periodId, Arg.Any<CancellationToken>())
            .Returns(false);

        _allocations.GetAvailableCapacityAsync(dormId, facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(1, 0);

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        await _allocations.Received(1).AddAsync(
            Arg.Is<DormAllocation>(a => a.UserId == earlyId),
            Arg.Any<CancellationToken>());
        await _allocations.DidNotReceive().AddAsync(
            Arg.Is<DormAllocation>(a => a.UserId == lateId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TiedPointsAndSubmission_LowerMatriculationCodeWins()
    {
        var dormId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var aId = Guid.NewGuid();
        var bId = Guid.NewGuid();

        var sameTime = new DateTime(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

        // M001 sorts before M002 lexicographically — M001 should win
        var (aUser, aRecord, aPref) = CreateStudent(aId, facultyId, Gender.Male, 10, "M001", dormId, 1, sameTime);
        var (bUser, bRecord, bPref) = CreateStudent(bId, facultyId, Gender.Male, 10, "M002", dormId, 1, sameTime);

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>())
            .Returns(new List<StudentRecord> { aRecord, bRecord });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { aUser, bUser });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { aPref, bPref });

        _allocations.FindActiveByUserAndPeriodAsync(Arg.Any<Guid>(), _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);
        _allocations.HasTerminalForUserAndPeriodAsync(Arg.Any<Guid>(), _periodId, Arg.Any<CancellationToken>())
            .Returns(false);

        _allocations.GetAvailableCapacityAsync(dormId, facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(1, 0);

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        await _allocations.Received(1).AddAsync(
            Arg.Is<DormAllocation>(a => a.UserId == aId),
            Arg.Any<CancellationToken>());
        await _allocations.DidNotReceive().AddAsync(
            Arg.Is<DormAllocation>(a => a.UserId == bId),
            Arg.Any<CancellationToken>());
    }

    // ─── Preference fallthrough ───────────────────────────────────────────

    [Fact]
    public async Task Handle_StudentFirstPreferenceFull_FallsThroughToSecond()
    {
        var dorm1Id = Guid.NewGuid();
        var dorm2Id = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var (user, record, pref1) = CreateStudent(userId, facultyId, Gender.Female, 10, "F001", dorm1Id, 1);
        var pref2 = DormPreference.Create(userId, _periodId, dorm2Id, 2);

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>()).Returns(new List<StudentRecord> { record });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<User> { user });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref1, pref2 });
        _allocations.FindActiveByUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);
        _allocations.HasTerminalForUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(false);

        // dorm1 full, dorm2 has capacity
        _allocations.GetAvailableCapacityAsync(dorm1Id, facultyId, Gender.Female, _periodId, Arg.Any<CancellationToken>())
            .Returns(0);
        _allocations.GetAvailableCapacityAsync(dorm2Id, facultyId, Gender.Female, _periodId, Arg.Any<CancellationToken>())
            .Returns(1);

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        await _allocations.Received(1).AddAsync(
            Arg.Is<DormAllocation>(a => a.UserId == userId && a.DormitoryId == dorm2Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllPreferencesFull_StudentRemainsUnallocated()
    {
        var dorm1Id = Guid.NewGuid();
        var dorm2Id = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var (user, record, pref1) = CreateStudent(userId, facultyId, Gender.Male, 8, "M001", dorm1Id, 1);
        var pref2 = DormPreference.Create(userId, _periodId, dorm2Id, 2);

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>()).Returns(new List<StudentRecord> { record });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<User> { user });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref1, pref2 });
        _allocations.FindActiveByUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);
        _allocations.HasTerminalForUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Both dorms are full
        _allocations.GetAvailableCapacityAsync(Arg.Any<Guid>(), facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(0);

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<AllocationCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    // ─── Gender / faculty filter via capacity ─────────────────────────────

    [Fact]
    public async Task Handle_GenderFilterRespected_DoesNotAllocateMaleToFemaleRoom()
    {
        var dormId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (user, record, pref) = CreateStudent(userId, facultyId, Gender.Male, 10, "M001", dormId, 1);

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>()).Returns(new List<StudentRecord> { record });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<User> { user });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref });
        _allocations.FindActiveByUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);
        _allocations.HasTerminalForUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Capacity check with Male gender returns 0 — room is female-only
        _allocations.GetAvailableCapacityAsync(dormId, facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(0);

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FacultyFilterRespected_DoesNotAllocateAcrossFaculties()
    {
        var dormId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (user, record, pref) = CreateStudent(userId, facultyId, Gender.Female, 10, "F001", dormId, 1);

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>()).Returns(new List<StudentRecord> { record });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<User> { user });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref });
        _allocations.FindActiveByUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);
        _allocations.HasTerminalForUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Capacity for this faculty returns 0 — dorm is assigned to another faculty
        _allocations.GetAvailableCapacityAsync(dormId, facultyId, Gender.Female, _periodId, Arg.Any<CancellationToken>())
            .Returns(0);

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
    }

    // ─── Pool exclusion rules ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_StudentAlreadyAllocated_NotInPool()
    {
        var dormId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (user, record, pref) = CreateStudent(userId, facultyId, Gender.Male, 10, "M001", dormId, 1);

        var existingAllocation = DormAllocation.Create(userId, dormId, _periodId, 1);

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>()).Returns(new List<StudentRecord> { record });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<User> { user });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref });

        // Student already has an active allocation
        _allocations.FindActiveByUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(existingAllocation);

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<AllocationCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DeclinedStudent_NotInPool()
    {
        var dormId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (user, record, pref) = CreateStudent(userId, facultyId, Gender.Male, 10, "M001", dormId, 1);

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>()).Returns(new List<StudentRecord> { record });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<User> { user });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref });
        _allocations.FindActiveByUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);

        // Student previously declined — terminal status recorded
        _allocations.HasTerminalForUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(true);

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<AllocationCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExpiredStudent_NotInPool()
    {
        var dormId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (user, record, pref) = CreateStudent(userId, facultyId, Gender.Female, 10, "F001", dormId, 1);

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>()).Returns(new List<StudentRecord> { record });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<User> { user });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref });
        _allocations.FindActiveByUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);

        // Student's previous allocation expired — terminal status recorded
        _allocations.HasTerminalForUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(true);

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<AllocationCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StudentWithoutPreferences_Skipped()
    {
        var facultyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var dormId = Guid.NewGuid();
        var (user, record, _) = CreateStudent(userId, facultyId, Gender.Male, 10, "M001", dormId, 1);

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>()).Returns(new List<StudentRecord> { record });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<User> { user });

        // No preferences returned for this student
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference>());

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<AllocationCreatedEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NonParticipantStudent_Skipped()
    {
        var facultyId = Guid.NewGuid();
        // A record with no UserId (student not yet participated)
        var record = StudentRecord.Create("X001", "Test", "User", 5, Gender.Male, facultyId, _periodId);
        // UserId remains null — not calling LinkToUser

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>()).Returns(new List<StudentRecord> { record });

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        // No user IDs → early return before fetching users
        await _users.DidNotReceive().GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>());
        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
    }

    // ─── Round number recorded ────────────────────────────────────────────

    [Fact]
    public async Task Handle_RoundNumberRecorded()
    {
        var dormId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        const int expectedRound = 3;
        var (user, record, pref) = CreateStudent(userId, facultyId, Gender.Male, 9, "M001", dormId, 1);

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>()).Returns(new List<StudentRecord> { record });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<User> { user });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref });
        _allocations.FindActiveByUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);
        _allocations.HasTerminalForUserAndPeriodAsync(userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(false);
        _allocations.GetAvailableCapacityAsync(dormId, facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(1);

        await Create().Handle(new RunAllocationRoundCommand(_periodId, expectedRound), CancellationToken.None);

        await _allocations.Received(1).AddAsync(
            Arg.Is<DormAllocation>(a => a.Round == expectedRound),
            Arg.Any<CancellationToken>());
    }

    // ─── Event publishing ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_PublishesAllocationCreatedEvent()
    {
        var dormId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var (user1, record1, pref1) = CreateStudent(userId1, facultyId, Gender.Male, 9, "M001", dormId, 1);
        var (user2, record2, pref2) = CreateStudent(userId2, facultyId, Gender.Male, 8, "M002", dormId, 1);

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _records.GetByPeriodAsync(_periodId, Arg.Any<CancellationToken>())
            .Returns(new List<StudentRecord> { record1, record2 });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user1, user2 });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref1, pref2 });

        _allocations.FindActiveByUserAndPeriodAsync(Arg.Any<Guid>(), _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);
        _allocations.HasTerminalForUserAndPeriodAsync(Arg.Any<Guid>(), _periodId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Both students get allocated
        _allocations.GetAvailableCapacityAsync(dormId, facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(5);

        await Create().Handle(new RunAllocationRoundCommand(_periodId, 1), CancellationToken.None);

        await _publisher.Received(2).Publish(
            Arg.Any<AllocationCreatedEvent>(),
            Arg.Any<CancellationToken>());
    }
}
