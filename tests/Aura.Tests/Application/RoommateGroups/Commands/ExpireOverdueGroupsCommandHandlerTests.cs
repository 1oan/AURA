using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Commands.ExpireOverdueGroups;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Events;
using Aura.Tests.Support;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Commands;

public class ExpireOverdueGroupsCommandHandlerTests
{
    private readonly IRoommateGroupRepository _groups = Substitute.For<IRoommateGroupRepository>();
    private readonly IGroupInvitationRepository _invitations = Substitute.For<IGroupInvitationRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly FakeTimeProvider _timeProvider = new(DateTimeOffset.Parse("2026-05-12T10:00:00Z"));

    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();

    private ExpireOverdueGroupsCommandHandler CreateHandler() =>
        new(_groups, _invitations, _timeProvider, _publisher);

    private static RoommateGroup CreateOverdueGroup(Guid periodId, Guid dormId)
    {
        var group = RoommateGroup.Create(periodId, dormId, Guid.NewGuid(), RoomSizePreference.ThreeBed);
        group.SetPrivateProperty("ExpiresAt", DateTime.UtcNow.AddHours(-1));
        return group;
    }

    [Fact]
    public async Task Handle_NoOverdue_NoOp()
    {
        _groups.GetExpiredOverdueAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<RoommateGroup>());

        await CreateHandler().Handle(new ExpireOverdueGroupsCommand(), CancellationToken.None);

        await _groups.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.DidNotReceive().Publish(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SingleOverdue_ExpiresAndRaisesEventAndCascadesInvitations()
    {
        var group = CreateOverdueGroup(_periodId, _dormId);
        var pendingInvitation = GroupInvitation.Create(group.Id, group.LeaderUserId, Guid.NewGuid());

        _groups.GetExpiredOverdueAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<RoommateGroup> { group });
        _invitations.GetPendingForGroupAsync(group.Id, Arg.Any<CancellationToken>())
            .Returns(new List<GroupInvitation> { pendingInvitation });

        await CreateHandler().Handle(new ExpireOverdueGroupsCommand(), CancellationToken.None);

        group.Status.Should().Be(GroupStatus.Expired);
        pendingInvitation.Status.Should().Be(InvitationStatus.Expired);
        await _groups.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _invitations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<GroupExpiredEvent>(e => e.GroupId == group.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MultipleOverdue_AllExpire()
    {
        var groupA = CreateOverdueGroup(_periodId, _dormId);
        var groupB = CreateOverdueGroup(_periodId, _dormId);

        _groups.GetExpiredOverdueAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new List<RoommateGroup> { groupA, groupB });
        _invitations.GetPendingForGroupAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<GroupInvitation>());

        await CreateHandler().Handle(new ExpireOverdueGroupsCommand(), CancellationToken.None);

        groupA.Status.Should().Be(GroupStatus.Expired);
        groupB.Status.Should().Be(GroupStatus.Expired);
        await _publisher.Received(2).Publish(Arg.Any<GroupExpiredEvent>(), Arg.Any<CancellationToken>());
    }
}
