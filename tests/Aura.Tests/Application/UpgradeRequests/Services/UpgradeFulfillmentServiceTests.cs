using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.UpgradeRequests.Services;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Tests.Support;
using FluentAssertions;
using MediatR;
using NSubstitute;

namespace Aura.Tests.Application.UpgradeRequests.Services;

public class UpgradeFulfillmentServiceTests
{
    private readonly IDormAllocationRepository _allocations = Substitute.For<IDormAllocationRepository>();
    private readonly IUpgradeRequestRepository _upgrades = Substitute.For<IUpgradeRequestRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDormPreferenceRepository _preferences = Substitute.For<IDormPreferenceRepository>();
    private readonly IStudentRecordRepository _records = Substitute.For<IStudentRecordRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _freedDormId = Guid.NewGuid();

    private UpgradeFulfillmentService Create() => new(
        _allocations, _upgrades, _users, _preferences, _records, _publisher);

    private (User user, StudentRecord record, DormPreference pref, UpgradeRequest request, DormAllocation active)
        CreateCandidate(Guid userId, Guid facultyId, Gender gender, int points, string code, Guid oldDormId, Guid? targetDormId = null)
    {
        var user = User.Create($"{code}@uaic.ro", "h");
        user.SetPrivateProperty("Id", userId);
        user.AssignToFaculty(facultyId);
        user.SetGender(gender);

        var record = StudentRecord.Create(code, "A", "B", points, gender, facultyId, _periodId);
        record.LinkToUser(userId);

        var pref = DormPreference.Create(userId, _periodId, _freedDormId, 1);

        var active = DormAllocation.Create(userId, oldDormId, _periodId, 1);
        active.Accept();

        var request = UpgradeRequest.Create(userId, _periodId, [targetDormId ?? _freedDormId]);

        return (user, record, pref, request, active);
    }

    [Fact]
    public async Task TryFulfill_NoUpgradeRequestsTargetingFreedDorm_NoOp()
    {
        _upgrades.GetActiveTargetingDormAsync(_freedDormId, _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<UpgradeRequest>());

        var fulfilled = await Create().TryFulfillForDormAsync(_freedDormId, _periodId, CancellationToken.None);

        fulfilled.Should().BeFalse();
        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryFulfill_OneRequest_Fulfilled()
    {
        var userId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var oldDormId = Guid.NewGuid();

        var (user, record, pref, request, oldAllocation) =
            CreateCandidate(userId, facultyId, Gender.Male, 9, "M001", oldDormId);

        _upgrades.GetActiveTargetingDormAsync(_freedDormId, _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<UpgradeRequest> { request });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });
        _records.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentRecord> { record });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref });
        _allocations.GetAvailableCapacityAsync(_freedDormId, facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(1);
        _allocations.GetActiveByUsersAndPeriodAsync(Arg.Any<IEnumerable<Guid>>(), _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { oldAllocation });

        var fulfilled = await Create().TryFulfillForDormAsync(_freedDormId, _periodId, CancellationToken.None);

        fulfilled.Should().BeTrue();
        oldAllocation.Status.Should().Be(AllocationStatus.Replaced);
        await _allocations.Received(1).AddAsync(
            Arg.Is<DormAllocation>(a => a.UserId == userId && a.DormitoryId == _freedDormId && a.Status == AllocationStatus.Accepted),
            Arg.Any<CancellationToken>());
        request.IsActive.Should().BeFalse();
        await _publisher.Received(1).Publish(
            Arg.Is<AllocationReplacedEvent>(e => e.UserId == userId && e.OldDormId == oldDormId && e.NewDormId == _freedDormId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryFulfill_TwoRequestsCompeting_HigherPointsWins()
    {
        var highId = Guid.NewGuid();
        var lowId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var oldDorm1 = Guid.NewGuid();
        var oldDorm2 = Guid.NewGuid();

        var (highUser, highRecord, highPref, highRequest, highActive) =
            CreateCandidate(highId, facultyId, Gender.Male, 9, "M001", oldDorm1);
        var (lowUser, lowRecord, lowPref, lowRequest, lowActive) =
            CreateCandidate(lowId, facultyId, Gender.Male, 7, "M002", oldDorm2);

        _upgrades.GetActiveTargetingDormAsync(_freedDormId, _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<UpgradeRequest> { highRequest, lowRequest });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { highUser, lowUser });
        _records.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentRecord> { highRecord, lowRecord });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { highPref, lowPref });
        _allocations.GetAvailableCapacityAsync(_freedDormId, facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(1);
        _allocations.GetActiveByUsersAndPeriodAsync(Arg.Any<IEnumerable<Guid>>(), _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { highActive, lowActive });

        await Create().TryFulfillForDormAsync(_freedDormId, _periodId, CancellationToken.None);

        highActive.Status.Should().Be(AllocationStatus.Replaced);
        lowActive.Status.Should().Be(AllocationStatus.Accepted);
        await _allocations.Received(1).AddAsync(
            Arg.Is<DormAllocation>(a => a.UserId == highId && a.DormitoryId == _freedDormId),
            Arg.Any<CancellationToken>());
        await _allocations.DidNotReceive().AddAsync(
            Arg.Is<DormAllocation>(a => a.UserId == lowId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryFulfill_FulfilledUpgradePublishesReplacedEvent()
    {
        var userId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var oldDormId = Guid.NewGuid();

        var (user, record, pref, request, oldAllocation) =
            CreateCandidate(userId, facultyId, Gender.Male, 9, "M001", oldDormId);

        _upgrades.GetActiveTargetingDormAsync(_freedDormId, _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<UpgradeRequest> { request });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });
        _records.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentRecord> { record });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref });
        _allocations.GetAvailableCapacityAsync(_freedDormId, facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(1);
        _allocations.GetActiveByUsersAndPeriodAsync(Arg.Any<IEnumerable<Guid>>(), _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { oldAllocation });

        await Create().TryFulfillForDormAsync(_freedDormId, _periodId, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<AllocationReplacedEvent>(e =>
                e.UserId == userId &&
                e.OldDormId == oldDormId &&
                e.NewDormId == _freedDormId &&
                e.AllocationPeriodId == _periodId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryFulfill_CapacityStillTaken_NoOp()
    {
        var userId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var oldDormId = Guid.NewGuid();

        var (user, record, pref, request, _) =
            CreateCandidate(userId, facultyId, Gender.Male, 9, "M001", oldDormId);

        _upgrades.GetActiveTargetingDormAsync(_freedDormId, _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<UpgradeRequest> { request });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });
        _records.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentRecord> { record });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref });
        _allocations.GetAvailableCapacityAsync(_freedDormId, facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(0);

        var fulfilled = await Create().TryFulfillForDormAsync(_freedDormId, _periodId, CancellationToken.None);

        fulfilled.Should().BeFalse();
        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryFulfill_RequestForDifferentGender_Skipped()
    {
        var userId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var oldDormId = Guid.NewGuid();

        var (user, record, pref, request, _) =
            CreateCandidate(userId, facultyId, Gender.Female, 9, "F001", oldDormId);

        _upgrades.GetActiveTargetingDormAsync(_freedDormId, _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<UpgradeRequest> { request });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });
        _records.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentRecord> { record });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref });
        _allocations.GetAvailableCapacityAsync(_freedDormId, facultyId, Gender.Female, _periodId, Arg.Any<CancellationToken>())
            .Returns(0);

        await Create().TryFulfillForDormAsync(_freedDormId, _periodId, CancellationToken.None);

        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryFulfill_RequestForDifferentFaculty_Skipped()
    {
        var userId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var oldDormId = Guid.NewGuid();

        var (user, record, pref, request, _) =
            CreateCandidate(userId, facultyId, Gender.Male, 9, "M001", oldDormId);

        _upgrades.GetActiveTargetingDormAsync(_freedDormId, _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<UpgradeRequest> { request });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });
        _records.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentRecord> { record });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref });
        _allocations.GetAvailableCapacityAsync(_freedDormId, facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(0);

        await Create().TryFulfillForDormAsync(_freedDormId, _periodId, CancellationToken.None);

        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryFulfill_StudentNoActiveAllocation_Skipped()
    {
        var userId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var oldDormId = Guid.NewGuid();

        var (user, record, pref, request, _) =
            CreateCandidate(userId, facultyId, Gender.Male, 9, "M001", oldDormId);

        _upgrades.GetActiveTargetingDormAsync(_freedDormId, _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<UpgradeRequest> { request });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });
        _records.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentRecord> { record });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref });
        _allocations.GetAvailableCapacityAsync(_freedDormId, facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(1);
        // No active allocation returned for the candidate user — should be skipped.
        _allocations.GetActiveByUsersAndPeriodAsync(Arg.Any<IEnumerable<Guid>>(), _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation>());

        await Create().TryFulfillForDormAsync(_freedDormId, _periodId, CancellationToken.None);

        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<INotification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sweep_NoActiveRequests_ReturnsZero()
    {
        _upgrades.GetActiveForPeriodAsync(_periodId, Arg.Any<CancellationToken>())
            .Returns(new List<UpgradeRequest>());

        var count = await Create().SweepActiveTargetsAsync(_periodId, CancellationToken.None);

        count.Should().Be(0);
        await _allocations.DidNotReceive().AddAsync(Arg.Any<DormAllocation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sweep_ActiveRequestAndUnfilledCapacity_FulfillsAndReturnsCount()
    {
        var userId = Guid.NewGuid();
        var facultyId = Guid.NewGuid();
        var oldDormId = Guid.NewGuid();

        var (user, record, pref, request, oldAllocation) =
            CreateCandidate(userId, facultyId, Gender.Male, 9, "M001", oldDormId);

        // Sweep flow: GetActiveForPeriod -> walk distinct target dorms -> TryFulfillForDorm per dorm
        _upgrades.GetActiveForPeriodAsync(_periodId, Arg.Any<CancellationToken>())
            .Returns(new List<UpgradeRequest> { request });
        _upgrades.GetActiveTargetingDormAsync(_freedDormId, _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<UpgradeRequest> { request });
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });
        _records.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentRecord> { record });
        _preferences.GetByPeriodAndUsersAsync(_periodId, Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormPreference> { pref });
        _allocations.GetAvailableCapacityAsync(_freedDormId, facultyId, Gender.Male, _periodId, Arg.Any<CancellationToken>())
            .Returns(1);
        _allocations.GetActiveByUsersAndPeriodAsync(Arg.Any<IEnumerable<Guid>>(), _periodId, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { oldAllocation });

        var count = await Create().SweepActiveTargetsAsync(_periodId, CancellationToken.None);

        count.Should().Be(1);
        oldAllocation.Status.Should().Be(AllocationStatus.Replaced);
        request.IsActive.Should().BeFalse();
    }
}
