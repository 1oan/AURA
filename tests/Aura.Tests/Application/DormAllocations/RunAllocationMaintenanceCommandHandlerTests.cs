using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Commands.RunAllocationMaintenance;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Aura.Tests.Application.DormAllocations;

public class RunAllocationMaintenanceCommandHandlerTests
{
    private readonly IDormAllocationRepository _repo = Substitute.For<IDormAllocationRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly FakeTimeProvider _time = new(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc));

    public RunAllocationMaintenanceCommandHandlerTests()
    {
        // Default both repo lookups to empty so tests focused on one path don't crash
        // on null from the other path. Individual tests override as needed.
        _repo.GetExpirablePendingAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation>());
        _repo.GetReminderDueAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation>());
    }

    private RunAllocationMaintenanceCommandHandler CreateHandler() =>
        new(_repo, _publisher, _time);

    private static DormAllocation PendingAllocation(Guid periodId)
    {
        return DormAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), periodId, 1);
    }

    [Fact]
    public async Task Handle_NoStalePending_NoOp()
    {
        await CreateHandler().Handle(new RunAllocationMaintenanceCommand(), CancellationToken.None);

        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<AllocationExpiredEvent>(), Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<AllocationReminderDueEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OneStalePending_ExpiresAndPublishes()
    {
        var periodId = Guid.NewGuid();
        var alloc = PendingAllocation(periodId);

        _repo.GetExpirablePendingAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { alloc });

        await CreateHandler().Handle(new RunAllocationMaintenanceCommand(), CancellationToken.None);

        alloc.Status.Should().Be(AllocationStatus.Expired);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<AllocationExpiredEvent>(e => e.AllocationId == alloc.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleStalePending_ExpiresAll()
    {
        var periodId = Guid.NewGuid();
        var alloc1 = PendingAllocation(periodId);
        var alloc2 = PendingAllocation(periodId);
        var alloc3 = PendingAllocation(periodId);

        _repo.GetExpirablePendingAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { alloc1, alloc2, alloc3 });

        await CreateHandler().Handle(new RunAllocationMaintenanceCommand(), CancellationToken.None);

        alloc1.Status.Should().Be(AllocationStatus.Expired);
        alloc2.Status.Should().Be(AllocationStatus.Expired);
        alloc3.Status.Should().Be(AllocationStatus.Expired);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(3).Publish(Arg.Any<AllocationExpiredEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OneReminderDue_MarksAndPublishesReminder()
    {
        var alloc = PendingAllocation(Guid.NewGuid());

        _repo.GetReminderDueAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { alloc });

        await CreateHandler().Handle(new RunAllocationMaintenanceCommand(), CancellationToken.None);

        alloc.ReminderSentAt.Should().NotBeNull();
        await _publisher.Received(1).Publish(
            Arg.Is<AllocationReminderDueEvent>(e => e.AllocationId == alloc.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleRemindersDue_PublishesAll()
    {
        var alloc1 = PendingAllocation(Guid.NewGuid());
        var alloc2 = PendingAllocation(Guid.NewGuid());

        _repo.GetReminderDueAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { alloc1, alloc2 });

        await CreateHandler().Handle(new RunAllocationMaintenanceCommand(), CancellationToken.None);

        alloc1.ReminderSentAt.Should().NotBeNull();
        alloc2.ReminderSentAt.Should().NotBeNull();
        await _publisher.Received(2).Publish(Arg.Any<AllocationReminderDueEvent>(), Arg.Any<CancellationToken>());
    }
}
