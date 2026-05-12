using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.UpgradeRequests.Commands.SubmitUpgradeRequest;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using MediatR;
using NSubstitute;

namespace Aura.Tests.Application.UpgradeRequests;

public class SubmitUpgradeRequestCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUpgradeRequestRepository _upgradeRequests = Substitute.For<IUpgradeRequestRepository>();
    private readonly IDormAllocationRepository _dormAllocations = Substitute.For<IDormAllocationRepository>();
    private readonly IFacultyRoomAllocationRepository _facultyRoomAllocations = Substitute.For<IFacultyRoomAllocationRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _facultyId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _currentDormId = Guid.NewGuid();
    private readonly Guid _targetDormA = Guid.NewGuid();
    private readonly Guid _targetDormB = Guid.NewGuid();

    private SubmitUpgradeRequestCommandHandler CreateHandler() =>
        new(_currentUser, _upgradeRequests, _dormAllocations, _facultyRoomAllocations, _users, _periods, _publisher);

    private User CreateParticipatedUser(Gender gender = Gender.Male)
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "hash");
        user.UpdateProfile("Ioan", "Virlescu");
        user.AssignToFaculty(_facultyId);
        user.SetGender(gender);
        user.SetPrivateProperty("Id", _userId);
        return user;
    }

    private DormAllocation CreateAcceptedAllocation()
    {
        var allocation = DormAllocation.Create(_userId, _currentDormId, _periodId, 1);
        allocation.Accept();
        return allocation;
    }

    private FacultyRoomAllocation CreateFacultyRoomAllocation(Guid dormitoryId, Gender gender)
    {
        var allocation = FacultyRoomAllocation.Create(_facultyId, Guid.NewGuid(), _periodId);
        var room = Room.Create("101", dormitoryId, 1, 3, gender);
        allocation.SetPrivateProperty("Room", room);
        return allocation;
    }

    private static AllocationPeriod MakePeriod(AllocationPeriodStatus status = AllocationPeriodStatus.Open)
    {
        var period = AllocationPeriod.Create(
            "2026-2027",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc),
            3);

        if (status >= AllocationPeriodStatus.Open) period.Activate();
        if (status >= AllocationPeriodStatus.Allocating) period.StartAllocating();
        if (status >= AllocationPeriodStatus.Closed) period.Close();
        return period;
    }

    private void SetupHappyPathBaseline(User? user = null, DormAllocation? allocation = null, List<FacultyRoomAllocation>? frAllocations = null, AllocationPeriod? period = null)
    {
        user ??= CreateParticipatedUser();
        allocation ??= CreateAcceptedAllocation();
        period ??= MakePeriod(AllocationPeriodStatus.Allocating);
        frAllocations ??= new List<FacultyRoomAllocation>
        {
            CreateFacultyRoomAllocation(_currentDormId, Gender.Male),
            CreateFacultyRoomAllocation(_targetDormA, Gender.Male),
            CreateFacultyRoomAllocation(_targetDormB, Gender.Male),
        };

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);
        _dormAllocations.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>()).Returns(allocation);
        _facultyRoomAllocations.GetByPeriodAndFacultyAsync(_periodId, _facultyId, Arg.Any<CancellationToken>()).Returns(frAllocations);
    }

    // ─── Guard clauses ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFoundException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var act = async () => await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_targetDormA]), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("*User not found*");
    }

    [Fact]
    public async Task Handle_NoActiveAllocation_ThrowsDomainException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateParticipatedUser());
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>())
            .Returns(MakePeriod(AllocationPeriodStatus.Allocating));
        _dormAllocations.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);

        var act = async () => await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_targetDormA]), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*active allocation*");
    }

    [Fact]
    public async Task Handle_AllocationNotAccepted_ThrowsDomainException()
    {
        var allocation = DormAllocation.Create(_userId, _currentDormId, _periodId, 1);
        // Pending, not Accepted
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateParticipatedUser());
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>())
            .Returns(MakePeriod(AllocationPeriodStatus.Allocating));
        _dormAllocations.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(allocation);

        var act = async () => await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_targetDormA]), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*accepted allocations*");
    }

    [Fact]
    public async Task Handle_TargetEqualsCurrentDormitory_ThrowsDomainException()
    {
        SetupHappyPathBaseline();

        var act = async () => await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_currentDormId]), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*differ from your current dormitory*");
    }

    [Fact]
    public async Task Handle_TargetDormHasNoFacultyRoomAllocation_ThrowsDomainException()
    {
        SetupHappyPathBaseline(frAllocations: new List<FacultyRoomAllocation>
        {
            CreateFacultyRoomAllocation(_currentDormId, Gender.Male),
        });

        var act = async () => await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_targetDormA]), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*not allocated to your faculty*");
    }

    [Fact]
    public async Task Handle_TargetDormHasNoRoomsMatchingGender_ThrowsDomainException()
    {
        SetupHappyPathBaseline(frAllocations: new List<FacultyRoomAllocation>
        {
            CreateFacultyRoomAllocation(_currentDormId, Gender.Male),
            CreateFacultyRoomAllocation(_targetDormA, Gender.Female),
        });

        var act = async () => await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_targetDormA]), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*matching your gender*");
    }

    [Fact]
    public async Task Handle_DuplicateDormitoryIds_ThrowsDomainException()
    {
        SetupHappyPathBaseline();

        // The validator catches duplicates first, but the domain layer (UpgradeRequest.Create) also enforces uniqueness.
        var act = async () => await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_targetDormA, _targetDormA]), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*unique*");
    }

    // ─── Cancel-and-recreate ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenExistingActiveRequest_CancelsItAndPublishesCancelledEvent()
    {
        SetupHappyPathBaseline();

        var existing = UpgradeRequest.Create(_userId, _periodId, new[] { _targetDormA });
        _upgradeRequests.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(existing);

        await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_targetDormA, _targetDormB]),
            CancellationToken.None);

        existing.IsActive.Should().BeFalse();
        await _publisher.Received(1).Publish(
            Arg.Is<UpgradeRequestCancelledEvent>(e =>
                e.UpgradeRequestId == existing.Id && e.UserId == _userId && e.AllocationPeriodId == _periodId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoExistingActiveRequest_DoesNotPublishCancelledEvent()
    {
        SetupHappyPathBaseline();
        _upgradeRequests.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns((UpgradeRequest?)null);

        await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_targetDormA]), CancellationToken.None);

        await _publisher.DidNotReceive().Publish(
            Arg.Any<UpgradeRequestCancelledEvent>(), Arg.Any<CancellationToken>());
    }

    // ─── Happy path ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_HappyPath_AddsSavesAndPublishesSubmittedEvent()
    {
        SetupHappyPathBaseline();

        UpgradeRequest? captured = null;
        await _upgradeRequests.AddAsync(
            Arg.Do<UpgradeRequest>(r => captured = r),
            Arg.Any<CancellationToken>());

        await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_targetDormA, _targetDormB]),
            CancellationToken.None);

        captured.Should().NotBeNull();
        await _upgradeRequests.Received(1).AddAsync(Arg.Any<UpgradeRequest>(), Arg.Any<CancellationToken>());
        await _upgradeRequests.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<UpgradeRequestSubmittedEvent>(e =>
                e.UpgradeRequestId == captured!.Id
                && e.UserId == _userId
                && e.AllocationPeriodId == _periodId
                && e.TargetDormIds.Count == 2
                && e.TargetDormIds[0] == _targetDormA
                && e.TargetDormIds[1] == _targetDormB),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_ReturnsNewRequestId()
    {
        SetupHappyPathBaseline();

        UpgradeRequest? captured = null;
        await _upgradeRequests.AddAsync(
            Arg.Do<UpgradeRequest>(r => captured = r),
            Arg.Any<CancellationToken>());

        var resultId = await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_targetDormA, _targetDormB]),
            CancellationToken.None);

        resultId.Should().NotBe(Guid.Empty);
        resultId.Should().Be(captured!.Id);
    }

    [Fact]
    public async Task Handle_HappyPath_TargetsStoredInSubmissionRankOrder()
    {
        SetupHappyPathBaseline();

        UpgradeRequest? captured = null;
        await _upgradeRequests.AddAsync(
            Arg.Do<UpgradeRequest>(r => captured = r),
            Arg.Any<CancellationToken>());

        // Submit with B first, then A — ranks must follow submission order
        await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_targetDormB, _targetDormA]),
            CancellationToken.None);

        captured.Should().NotBeNull();
        var orderedTargets = captured!.Targets.OrderBy(t => t.Rank).ToList();
        orderedTargets[0].DormitoryId.Should().Be(_targetDormB);
        orderedTargets[0].Rank.Should().Be(1);
        orderedTargets[1].DormitoryId.Should().Be(_targetDormA);
        orderedTargets[1].Rank.Should().Be(2);
    }

    [Fact]
    public async Task Handle_HappyPath_SaveBeforePublish()
    {
        SetupHappyPathBaseline();

        var callOrder = new List<string>();
        _upgradeRequests.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("save"));
        _publisher.Publish(Arg.Any<UpgradeRequestSubmittedEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("publish-submitted"));
        _publisher.Publish(Arg.Any<UpgradeRequestCancelledEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => callOrder.Add("publish-cancelled"));

        // Set up an existing active request so both events should fire after save.
        var existing = UpgradeRequest.Create(_userId, _periodId, new[] { _targetDormA });
        _upgradeRequests.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>())
            .Returns(existing);

        await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_targetDormA, _targetDormB]),
            CancellationToken.None);

        callOrder.Should().Equal("save", "publish-cancelled", "publish-submitted");
    }

    [Fact]
    public async Task Handle_UserHasNullFacultyOrGender_ThrowsDomainException()
    {
        var user = User.Create("ioan.virlescu@student.uaic.ro", "hash");
        user.UpdateProfile("Ioan", "Virlescu");
        // Intentionally do NOT call AssignToFaculty / SetGender — the user has not participated.
        user.SetPrivateProperty("Id", _userId);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(user);

        var act = async () => await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_targetDormA]), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*participate*");
    }

    [Theory]
    [InlineData(AllocationPeriodStatus.Draft)]
    [InlineData(AllocationPeriodStatus.Closed)]
    public async Task Handle_PeriodNotInOpenOrAllocating_ThrowsDomainException(AllocationPeriodStatus status)
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(CreateParticipatedUser());
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(MakePeriod(status));

        var act = async () => await CreateHandler().Handle(
            new SubmitUpgradeRequestCommand(_periodId, [_targetDormA]), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*open or allocating*");
    }
}
