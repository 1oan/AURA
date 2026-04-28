using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
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

public class AllocationExpirationServiceTests
{
    private readonly IDormAllocationRepository _repo = Substitute.For<IDormAllocationRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private IServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _repo);
        services.AddScoped(_ => _publisher);
        return services.BuildServiceProvider();
    }

    private static DormAllocation PendingAllocation(Guid periodId)
    {
        return DormAllocation.Create(Guid.NewGuid(), Guid.NewGuid(), periodId, 1);
    }

    [Fact]
    public async Task Tick_NoStalePending_NoOp()
    {
        _repo.GetExpirablePendingAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation>());

        var time = new FakeTimeProvider(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc));
        var svc = new AllocationExpirationService(BuildProvider(), NullLogger<AllocationExpirationService>.Instance, time);

        await svc.RunOneTickAsync(CancellationToken.None);

        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<AllocationExpiredEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tick_OneStalePending_ExpiresAndPublishes()
    {
        var periodId = Guid.NewGuid();
        var alloc = PendingAllocation(periodId);

        _repo.GetExpirablePendingAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { alloc });

        var time = new FakeTimeProvider(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc));
        var svc = new AllocationExpirationService(BuildProvider(), NullLogger<AllocationExpirationService>.Instance, time);

        await svc.RunOneTickAsync(CancellationToken.None);

        alloc.Status.Should().Be(AllocationStatus.Expired);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<AllocationExpiredEvent>(e => e.AllocationId == alloc.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tick_MultipleStalePending_ExpiresAll()
    {
        var periodId = Guid.NewGuid();
        var alloc1 = PendingAllocation(periodId);
        var alloc2 = PendingAllocation(periodId);
        var alloc3 = PendingAllocation(periodId);

        _repo.GetExpirablePendingAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<DormAllocation> { alloc1, alloc2, alloc3 });

        var time = new FakeTimeProvider(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc));
        var svc = new AllocationExpirationService(BuildProvider(), NullLogger<AllocationExpirationService>.Instance, time);

        await svc.RunOneTickAsync(CancellationToken.None);

        alloc1.Status.Should().Be(AllocationStatus.Expired);
        alloc2.Status.Should().Be(AllocationStatus.Expired);
        alloc3.Status.Should().Be(AllocationStatus.Expired);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(3).Publish(Arg.Any<AllocationExpiredEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Tick_RepoThrows_NextTickStillFires()
    {
        _repo.GetExpirablePendingAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns<Task<List<DormAllocation>>>(_ => throw new InvalidOperationException("db error"));

        var time = new FakeTimeProvider(new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc));
        var svc = new AllocationExpirationService(BuildProvider(), NullLogger<AllocationExpirationService>.Instance, time);

        var act = async () => await svc.RunOneTickAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
