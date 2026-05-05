using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Commands.RunAllocationRound;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Infrastructure.Scheduling;
using Aura.Tests.Support;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Aura.Tests.Application.DormAllocations;

public class AllocationSchedulerServiceTests
{
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();
    private readonly IDormAllocationRepository _allocations = Substitute.For<IDormAllocationRepository>();
    private readonly ISender _sender = Substitute.For<ISender>();

    private IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _periods);
        services.AddScoped(_ => _allocations);
        services.AddScoped(_ => _sender);
        return services.BuildServiceProvider();
    }

    private AllocationPeriod AllocatingPeriod(Guid periodId)
    {
        var p = AllocationPeriod.Create("t",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc), 3);
        p.SetPrivateProperty("Id", periodId);
        p.Activate();
        p.StartAllocating();
        return p;
    }

    [Fact]
    public async Task Tick_NoPeriodsAllocating_NoCommandsSent()
    {
        _periods.GetAllocatingDueAtAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<AllocationPeriod>());

        var time = new FakeTimeProvider(new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc));
        var svc = new AllocationSchedulerService(BuildProvider(), NullLogger<AllocationSchedulerService>.Instance, time);

        await svc.RunOneTickAsync(CancellationToken.None);

        await _sender.DidNotReceive().Send(Arg.Any<RunAllocationRoundCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tick_Round1DueNotRun_SendsRound1()
    {
        var periodId = Guid.NewGuid();
        var period = AllocatingPeriod(periodId);

        _periods.GetAllocatingDueAtAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<AllocationPeriod> { period });
        _allocations.GetMaxRoundAsync(periodId, Arg.Any<CancellationToken>()).Returns(0);

        var now = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);  // exactly Round1Date
        var time = new FakeTimeProvider(now);
        var svc = new AllocationSchedulerService(BuildProvider(), NullLogger<AllocationSchedulerService>.Instance, time);

        await svc.RunOneTickAsync(CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<RunAllocationRoundCommand>(c => c.AllocationPeriodId == periodId && c.Round == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tick_Round1DateInFuture_NoCommandsSent()
    {
        var periodId = Guid.NewGuid();
        var period = AllocatingPeriod(periodId);

        // GetAllocatingDueAtAsync should not return this period at all (since Round1Date > now);
        // simulate that by returning empty list
        _periods.GetAllocatingDueAtAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<AllocationPeriod>());

        var now = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);  // before Round1Date
        var time = new FakeTimeProvider(now);
        var svc = new AllocationSchedulerService(BuildProvider(), NullLogger<AllocationSchedulerService>.Instance, time);

        await svc.RunOneTickAsync(CancellationToken.None);

        await _sender.DidNotReceive().Send(Arg.Any<RunAllocationRoundCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tick_TwoRoundsDue_SendsBoth()
    {
        var periodId = Guid.NewGuid();
        var period = AllocatingPeriod(periodId);

        _periods.GetAllocatingDueAtAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<AllocationPeriod> { period });
        _allocations.GetMaxRoundAsync(periodId, Arg.Any<CancellationToken>()).Returns(0);

        // Round1Date = 2026-09-15, ResponseWindowDays = 3
        // elapsed = 3 days exactly: floor(3/3)+1 = 1+1 = 2
        var now = new DateTime(2026, 9, 18, 0, 0, 0, DateTimeKind.Utc);  // 3 days after Round1
        var time = new FakeTimeProvider(now);
        var svc = new AllocationSchedulerService(BuildProvider(), NullLogger<AllocationSchedulerService>.Instance, time);

        await svc.RunOneTickAsync(CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<RunAllocationRoundCommand>(c => c.Round == 1),
            Arg.Any<CancellationToken>());
        await _sender.Received(1).Send(
            Arg.Is<RunAllocationRoundCommand>(c => c.Round == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tick_AlreadyOnCurrentRound_NoCommands()
    {
        var periodId = Guid.NewGuid();
        var period = AllocatingPeriod(periodId);

        _periods.GetAllocatingDueAtAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<AllocationPeriod> { period });
        _allocations.GetMaxRoundAsync(periodId, Arg.Any<CancellationToken>()).Returns(1);

        var now = new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc);  // expected round = 1, already at 1
        var time = new FakeTimeProvider(now);
        var svc = new AllocationSchedulerService(BuildProvider(), NullLogger<AllocationSchedulerService>.Instance, time);

        await svc.RunOneTickAsync(CancellationToken.None);

        await _sender.DidNotReceive().Send(Arg.Any<RunAllocationRoundCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tick_HandlerThrows_NextTickStillFires()
    {
        _periods.GetAllocatingDueAtAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns<Task<List<AllocationPeriod>>>(_ => throw new InvalidOperationException("boom"));

        var time = new FakeTimeProvider(new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc));
        var svc = new AllocationSchedulerService(BuildProvider(), NullLogger<AllocationSchedulerService>.Instance, time);

        var act = async () => await svc.RunOneTickAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
