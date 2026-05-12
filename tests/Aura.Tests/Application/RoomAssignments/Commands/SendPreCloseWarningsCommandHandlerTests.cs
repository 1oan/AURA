using Aura.Application.Common.Interfaces;
using Aura.Application.RoomAssignments.Commands.SendPreCloseWarnings;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Tests.Support;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Aura.Tests.Application.RoomAssignments.Commands;

public class SendPreCloseWarningsCommandHandlerTests
{
    private readonly IAllocationPeriodRepository _periodRepository = Substitute.For<IAllocationPeriodRepository>();
    private readonly IDormAllocationRepository _dormAllocationRepository = Substitute.For<IDormAllocationRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();

    private readonly Guid _userId = Guid.NewGuid();

    private SendPreCloseWarningsCommandHandler CreateHandler(TimeProvider timeProvider) =>
        new(_periodRepository, _dormAllocationRepository, _userRepository, _emailService, timeProvider);

    [Fact]
    public async Task Handle_AtCheckpoint_SendsWarningEmails()
    {
        var now = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);
        var timeProvider = new FakeTimeProvider(now);

        var period = AllocationPeriod.Create(
            "Test Period",
            now.AddDays(-10),
            now.AddHours(72.5),
            now.AddDays(-9),
            3);
        period.Activate();
        period.StartAllocating();

        var dormId = Guid.NewGuid();
        var alloc = DormAllocation.Create(_userId, dormId, period.Id, 1);
        alloc.SetPrivateProperty("Status", AllocationStatus.Accepted);

        var dorm = Dormitory.Create("Casa Studentului", Guid.NewGuid());
        alloc.SetPrivateProperty("Dormitory", dorm);

        var user = User.Create("student@uaic.ro", "hash");
        user.SetPrivateProperty("Id", _userId);
        user.SetPrivateProperty("FirstName", "Ana");

        _periodRepository.ListAllocatingAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AllocationPeriod> { period });
        _dormAllocationRepository.ListAcceptedWithoutRoomAsync(period.Id, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { alloc }.AsReadOnly() as IReadOnlyList<DormAllocation>);
        _userRepository.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { user });

        await CreateHandler(timeProvider).Handle(new SendPreCloseWarningsCommand(), CancellationToken.None);

        await _dormAllocationRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailService.Received(1).SendPreCloseWarningAsync(
            "student@uaic.ro", "Ana", "Casa Studentului", period.EndDate, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotAtCheckpoint_DoesNotSendEmails()
    {
        var now = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);
        var timeProvider = new FakeTimeProvider(now);

        var period = AllocationPeriod.Create(
            "Test Period",
            now.AddDays(-10),
            now.AddHours(36),
            now.AddDays(-9),
            3);
        period.Activate();
        period.StartAllocating();

        _periodRepository.ListAllocatingAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AllocationPeriod> { period });

        await CreateHandler(timeProvider).Handle(new SendPreCloseWarningsCommand(), CancellationToken.None);

        await _dormAllocationRepository.DidNotReceive()
            .ListAcceptedWithoutRoomAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _emailService.DidNotReceive().SendPreCloseWarningAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllAlreadyWarned_SkipsEmails()
    {
        var now = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);
        var timeProvider = new FakeTimeProvider(now);

        var period = AllocationPeriod.Create(
            "Test Period",
            now.AddDays(-10),
            now.AddHours(24.5),
            now.AddDays(-9),
            3);
        period.Activate();
        period.StartAllocating();

        var dormId = Guid.NewGuid();
        var alloc = DormAllocation.Create(_userId, dormId, period.Id, 1);
        alloc.SetPrivateProperty("Status", AllocationStatus.Accepted);
        alloc.SetPrivateProperty("LastPreCloseWarningSentAt", now.AddHours(-2));

        _periodRepository.ListAllocatingAsync(Arg.Any<CancellationToken>())
            .Returns(new List<AllocationPeriod> { period });
        _dormAllocationRepository.ListAcceptedWithoutRoomAsync(period.Id, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { alloc }.AsReadOnly() as IReadOnlyList<DormAllocation>);

        await CreateHandler(timeProvider).Handle(new SendPreCloseWarningsCommand(), CancellationToken.None);

        await _dormAllocationRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _emailService.DidNotReceive().SendPreCloseWarningAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }
}
