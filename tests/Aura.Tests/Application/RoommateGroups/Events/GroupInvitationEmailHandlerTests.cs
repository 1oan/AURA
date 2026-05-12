using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Events;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Tests.Support;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Events;

public class GroupInvitationEmailHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRoommateGroupRepository _groups = Substitute.For<IRoommateGroupRepository>();
    private readonly IDormitoryRepository _dorms = Substitute.For<IDormitoryRepository>();
    private readonly IEmailService _email = Substitute.For<IEmailService>();
    private readonly ILogger<GroupInvitationEmailHandler> _logger = Substitute.For<ILogger<GroupInvitationEmailHandler>>();

    private readonly Guid _invitationId = Guid.NewGuid();
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _inviterId = Guid.NewGuid();
    private readonly Guid _inviteeId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();

    private GroupInvitationEmailHandler Create() => new(_users, _groups, _dorms, _email, _logger);

    [Fact]
    public async Task Handle_AllDataPresent_CallsSendGroupInvitationAsync()
    {
        var inviter = User.Create("inv@uaic.ro", "hash");
        inviter.SetPrivateProperty("Id", _inviterId);
        inviter.SetPrivateProperty("FirstName", "Maria");

        var invitee = User.Create("ee@uaic.ro", "hash");
        invitee.SetPrivateProperty("Id", _inviteeId);
        invitee.SetPrivateProperty("FirstName", "Andrei");

        var campus = Campus.Create("Tudor");
        campus.SetPrivateProperty("Id", Guid.NewGuid());
        var dorm = Dormitory.Create("C1", campus.Id);
        dorm.SetPrivateProperty("Id", _dormId);

        var group = RoommateGroup.Create(_periodId, _dormId, _inviterId, RoomSizePreference.ThreeBed);

        _users.FindByIdAsync(_inviteeId, Arg.Any<CancellationToken>()).Returns(invitee);
        _users.FindByIdAsync(_inviterId, Arg.Any<CancellationToken>()).Returns(inviter);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);
        _dorms.FindByIdAsync(_dormId, Arg.Any<CancellationToken>()).Returns(dorm);

        await Create().Handle(
            new GroupInvitationCreatedEvent(_invitationId, _inviteeId, _inviterId, _groupId),
            CancellationToken.None);

        await _email.Received(1).SendGroupInvitationAsync(
            "ee@uaic.ro", "Andrei", "Maria", "C1", 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InviteeMissing_DoesNotCallEmail()
    {
        _users.FindByIdAsync(_inviteeId, Arg.Any<CancellationToken>()).Returns((User?)null);

        await Create().Handle(
            new GroupInvitationCreatedEvent(_invitationId, _inviteeId, _inviterId, _groupId),
            CancellationToken.None);

        await _email.DidNotReceive().SendGroupInvitationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }
}
