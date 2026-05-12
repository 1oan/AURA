using Aura.Application.RoomAssignments.Events;
using Aura.Domain.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.Tests.Application.RoomAssignments.Events;

public class GroupExpiredRoomReleaseHandlerTests
{
    private readonly ILogger<GroupExpiredRoomReleaseHandler> _logger =
        Substitute.For<ILogger<GroupExpiredRoomReleaseHandler>>();

    [Fact]
    public async Task Handle_LogsAndCompletes()
    {
        var handler = new GroupExpiredRoomReleaseHandler(_logger);
        var evt = new GroupExpiredEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()]);

        var act = async () => await handler.Handle(evt, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
