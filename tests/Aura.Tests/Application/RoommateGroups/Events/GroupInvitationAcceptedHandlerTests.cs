using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Events;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Events;

public class GroupInvitationAcceptedHandlerTests
{
    private readonly IRoommateGroupRepository _groups = Substitute.For<IRoommateGroupRepository>();
    private readonly ILogger<GroupInvitationAcceptedHandler> _logger = Substitute.For<ILogger<GroupInvitationAcceptedHandler>>();

    private readonly Guid _invitationId = Guid.NewGuid();
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _leaderId = Guid.NewGuid();
    private readonly Guid _inviteeId = Guid.NewGuid();

    private GroupInvitationAcceptedHandler Create() => new(_groups, _logger);

    [Fact]
    public async Task Handle_GroupFound_AddsMemberAndSaves()
    {
        var group = RoommateGroup.Create(_periodId, _dormId, _leaderId, RoomSizePreference.ThreeBed);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        await Create().Handle(
            new GroupInvitationAcceptedEvent(_invitationId, _groupId, _inviteeId),
            CancellationToken.None);

        group.Members.Should().Contain(m => m.UserId == _inviteeId);
        await _groups.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GroupMissing_DoesNotSave()
    {
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns((RoommateGroup?)null);

        await Create().Handle(
            new GroupInvitationAcceptedEvent(_invitationId, _groupId, _inviteeId),
            CancellationToken.None);

        await _groups.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
