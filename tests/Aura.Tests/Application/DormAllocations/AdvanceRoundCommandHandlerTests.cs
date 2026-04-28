using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.DormAllocations.Commands.AdvanceRound;
using Aura.Application.DormAllocations.Commands.RunAllocationRound;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using MediatR;
using NSubstitute;

namespace Aura.Tests.Application.DormAllocations;

public class AdvanceRoundCommandHandlerTests
{
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();
    private readonly IDormAllocationRepository _allocations = Substitute.For<IDormAllocationRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly ISender _sender = Substitute.For<ISender>();

    private readonly Guid _periodId = Guid.NewGuid();

    private AdvanceRoundCommandHandler Create() => new(_periods, _allocations, _publisher, _sender);

    private AllocationPeriod AllocatingPeriod()
    {
        var p = AllocationPeriod.Create("t",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc), 3);
        p.SetPrivateProperty("Id", _periodId);
        p.Activate();
        p.StartAllocating();
        return p;
    }

    [Fact]
    public async Task Handle_FirstInvocation_RunsRound1()
    {
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _allocations.GetMaxRoundAsync(_periodId, Arg.Any<CancellationToken>()).Returns(0);
        _allocations.GetPendingFromPriorRoundsAsync(_periodId, 1, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation>());

        await Create().Handle(new AdvanceRoundCommand(_periodId), CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<RunAllocationRoundCommand>(c => c.AllocationPeriodId == _periodId && c.Round == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SecondInvocation_RunsRound2()
    {
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _allocations.GetMaxRoundAsync(_periodId, Arg.Any<CancellationToken>()).Returns(1);
        _allocations.GetPendingFromPriorRoundsAsync(_periodId, 2, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation>());

        await Create().Handle(new AdvanceRoundCommand(_periodId), CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<RunAllocationRoundCommand>(c => c.Round == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PeriodNotAllocating_Throws()
    {
        var period = AllocationPeriod.Create("t",
            new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc), 3);
        period.SetPrivateProperty("Id", _periodId);
        // Status remains Draft — no Activate() / StartAllocating()

        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(period);

        var act = async () => await Create().Handle(
            new AdvanceRoundCommand(_periodId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Allocating*");
    }

    [Fact]
    public async Task Handle_WithNoPendingFromPriorRounds_NoExpirationEvents()
    {
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _allocations.GetMaxRoundAsync(_periodId, Arg.Any<CancellationToken>()).Returns(1);
        _allocations.GetPendingFromPriorRoundsAsync(_periodId, 2, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation>());

        await Create().Handle(new AdvanceRoundCommand(_periodId), CancellationToken.None);

        await _publisher.DidNotReceive().Publish(Arg.Any<AllocationExpiredEvent>(), Arg.Any<CancellationToken>());
        await _allocations.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _sender.Received(1).Send(
            Arg.Is<RunAllocationRoundCommand>(c => c.Round == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPendingFromPriorRounds_ExpiresAndPublishes()
    {
        _periods.FindByIdAsync(_periodId, Arg.Any<CancellationToken>()).Returns(AllocatingPeriod());
        _allocations.GetMaxRoundAsync(_periodId, Arg.Any<CancellationToken>()).Returns(1);

        var alloc1 = DormAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), _periodId, 1);
        var alloc2 = DormAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), _periodId, 1);
        _allocations.GetPendingFromPriorRoundsAsync(_periodId, 2, Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { alloc1, alloc2 });

        await Create().Handle(new AdvanceRoundCommand(_periodId), CancellationToken.None);

        alloc1.Status.Should().Be(AllocationStatus.Expired);
        alloc2.Status.Should().Be(AllocationStatus.Expired);
        await _allocations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<AllocationExpiredEvent>(e => e.AllocationId == alloc1.Id),
            Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<AllocationExpiredEvent>(e => e.AllocationId == alloc2.Id),
            Arg.Any<CancellationToken>());
    }
}
