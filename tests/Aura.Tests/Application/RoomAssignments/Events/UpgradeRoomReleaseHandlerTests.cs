using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.RoomAssignments.Events;
using Aura.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.Tests.Application.RoomAssignments.Events;

public class UpgradeRoomReleaseHandlerTests
{
    private readonly IRoomAssignmentRepository _repo = Substitute.For<IRoomAssignmentRepository>();
    private readonly ILogger<UpgradeRoomReleaseHandler> _logger =
        Substitute.For<ILogger<UpgradeRoomReleaseHandler>>();

    private UpgradeRoomReleaseHandler Create() => new(_repo, _logger);

    [Fact]
    public async Task Handle_RoomAssignmentExists_RemovesAndSaves()
    {
        var userId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var assignment = RoomAssignment.Create(userId, Guid.NewGuid(), periodId);
        _repo.FindByUserAndPeriodAsync(userId, periodId, Arg.Any<CancellationToken>())
            .Returns(assignment);

        var evt = new AllocationReplacedEvent(userId, Guid.NewGuid(), Guid.NewGuid(), periodId);

        await Create().Handle(evt, CancellationToken.None);

        _repo.Received(1).Remove(assignment);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoRoomAssignment_DoesNothing()
    {
        var userId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        _repo.FindByUserAndPeriodAsync(userId, periodId, Arg.Any<CancellationToken>())
            .Returns((RoomAssignment?)null);

        var evt = new AllocationReplacedEvent(userId, Guid.NewGuid(), Guid.NewGuid(), periodId);

        await Create().Handle(evt, CancellationToken.None);

        _repo.DidNotReceive().Remove(Arg.Any<RoomAssignment>());
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
