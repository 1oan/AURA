using Aura.Application.Common.Interfaces;
using Aura.Application.RoomAssignments.Queries.GetActivePeriodCountdown;
using Aura.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Aura.Tests.Application.RoomAssignments.Queries;

public class GetActivePeriodCountdownQueryHandlerTests
{
    private readonly IAllocationPeriodRepository _periods = Substitute.For<IAllocationPeriodRepository>();

    private GetActivePeriodCountdownQueryHandler CreateHandler(TimeProvider timeProvider) =>
        new(_periods, timeProvider);

    [Fact]
    public async Task Handle_NoPeriod_ReturnsNull()
    {
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        _periods.GetActiveAllocatingAsync(Arg.Any<CancellationToken>())
            .Returns((AllocationPeriod?)null);

        var result = await CreateHandler(timeProvider).Handle(new GetActivePeriodCountdownQuery(), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithPeriod_ReturnsDto_WithCorrectFields()
    {
        var now = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);

        var period = AllocationPeriod.Create(
            "Summer 2026",
            now.UtcDateTime.AddDays(-10),
            now.UtcDateTime.AddDays(2),
            now.UtcDateTime.AddDays(-9),
            3);

        _periods.GetActiveAllocatingAsync(Arg.Any<CancellationToken>()).Returns(period);

        var result = await CreateHandler(timeProvider).Handle(new GetActivePeriodCountdownQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.AllocationPeriodId.Should().Be(period.Id);
        result.PeriodName.Should().Be("Summer 2026");
        result.ClosingAtUtc.Should().Be(period.EndDate);
    }

    [Fact]
    public async Task Handle_WithPeriod_HoursRemainingCalculatedFromTimeProvider()
    {
        var now = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(now);

        var period = AllocationPeriod.Create(
            "Summer 2026",
            now.UtcDateTime.AddDays(-10),
            now.UtcDateTime.AddDays(2),
            now.UtcDateTime.AddDays(-9),
            3);

        _periods.GetActiveAllocatingAsync(Arg.Any<CancellationToken>()).Returns(period);

        var result = await CreateHandler(timeProvider).Handle(new GetActivePeriodCountdownQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.HoursRemaining.Should().BeApproximately(48.0, 1.0);
    }
}
