using Aura.Application.Common.Events;
using Aura.Application.DormAllocations.Events;
using Aura.Application.UpgradeRequests.Services;
using NSubstitute;

namespace Aura.Tests.Application.DormAllocations;

public class CapacityFreedNotificationHandlerTests
{
    private readonly IUpgradeFulfillmentService _fulfillment = Substitute.For<IUpgradeFulfillmentService>();

    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _allocationId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    private CapacityFreedNotificationHandler Create() => new(_fulfillment);

    [Fact]
    public async Task Handle_AllocationDeclined_DelegatesWithDormitoryId()
    {
        await Create().Handle(
            new AllocationDeclinedEvent(_allocationId, _userId, _dormId, _periodId),
            CancellationToken.None);

        await _fulfillment.Received(1).TryFulfillForDormAsync(_dormId, _periodId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllocationExpired_DelegatesWithDormitoryId()
    {
        await Create().Handle(
            new AllocationExpiredEvent(_allocationId, _userId, _dormId, _periodId),
            CancellationToken.None);

        await _fulfillment.Received(1).TryFulfillForDormAsync(_dormId, _periodId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllocationReplaced_DelegatesWithOldDormId()
    {
        var oldDormId = Guid.NewGuid();
        var newDormId = Guid.NewGuid();

        await Create().Handle(
            new AllocationReplacedEvent(_userId, oldDormId, newDormId, _periodId),
            CancellationToken.None);

        // The old dorm is what just freed capacity — that's where to look for upgrade candidates.
        await _fulfillment.Received(1).TryFulfillForDormAsync(oldDormId, _periodId, Arg.Any<CancellationToken>());
    }
}
