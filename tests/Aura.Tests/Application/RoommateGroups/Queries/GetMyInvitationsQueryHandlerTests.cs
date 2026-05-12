using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Queries.GetMyInvitations;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Queries;

public class GetMyInvitationsQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IGroupInvitationRepository _invitations = Substitute.For<IGroupInvitationRepository>();
    private readonly IRoommateGroupRepository _groups = Substitute.For<IRoommateGroupRepository>();
    private readonly IDormitoryRepository _dormitories = Substitute.For<IDormitoryRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _inviterId = Guid.NewGuid();
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();

    private GetMyInvitationsQueryHandler CreateHandler() =>
        new(_currentUser, _invitations, _groups, _dormitories, _users);

    [Fact]
    public async Task Handle_NoPendingInvitations_ReturnsEmpty()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _invitations.GetPendingForUserAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<GroupInvitation>());

        var result = await CreateHandler().Handle(new GetMyInvitationsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithPendingInvitation_ReturnsMappedDto()
    {
        var invitation = GroupInvitation.Create(_groupId, _inviterId, _userId);
        invitation.SetPrivateProperty("Id", Guid.NewGuid());

        var group = RoommateGroup.Create(_periodId, _dormId, _inviterId, RoomSizePreference.TwoBed);
        group.SetPrivateProperty("Id", _groupId);

        var campusId = Guid.NewGuid();
        var dorm = Dormitory.Create("Casa Studentului", campusId);
        dorm.SetPrivateProperty("Id", _dormId);

        var inviter = User.Create("inviter@uaic.ro", "hash");
        inviter.SetPrivateProperty("Id", _inviterId);
        inviter.UpdateProfile("Mihai", "Popescu");

        _currentUser.GetCurrentUserId().Returns(_userId);
        _invitations.GetPendingForUserAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<GroupInvitation> { invitation });
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);
        _dormitories.FindByIdAsync(_dormId, Arg.Any<CancellationToken>()).Returns(dorm);
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<User> { inviter });

        var result = await CreateHandler().Handle(new GetMyInvitationsQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].DormitoryName.Should().Be("Casa Studentului");
        result[0].InviterFirstName.Should().Be("Mihai");
        result[0].RoomSizePreference.Should().Be(2);
    }

    [Fact]
    public async Task Handle_GroupNotFound_FiltersOutInvitation()
    {
        var invitation = GroupInvitation.Create(_groupId, _inviterId, _userId);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _invitations.GetPendingForUserAsync(_userId, Arg.Any<CancellationToken>()).Returns(new List<GroupInvitation> { invitation });
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns((RoommateGroup?)null);
        _users.GetByIdsAsync(Arg.Any<List<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<User>());

        var result = await CreateHandler().Handle(new GetMyInvitationsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
