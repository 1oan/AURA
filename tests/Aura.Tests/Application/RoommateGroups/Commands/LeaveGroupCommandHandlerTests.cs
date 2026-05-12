using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Commands.LeaveGroup;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Commands;

public class LeaveGroupCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IRoommateGroupRepository _groups = Substitute.For<IRoommateGroupRepository>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _leaderId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _groupId = Guid.NewGuid();

    private LeaveGroupCommandHandler CreateHandler() =>
        new(_currentUser, _groups);

    private RoommateGroup CreateGroupWithMembers(params Guid[] additionalMembers)
    {
        var group = RoommateGroup.Create(_periodId, _dormId, _leaderId, RoomSizePreference.ThreeBed);
        group.SetPrivateProperty("Id", _groupId);
        foreach (var member in additionalMembers)
            group.AddMember(member);
        return group;
    }

    [Fact]
    public async Task Handle_HappyPath_RemovesMemberAndSaves()
    {
        var group = CreateGroupWithMembers(_userId);
        _currentUser.GetCurrentUserId().Returns(_userId);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        await CreateHandler().Handle(
            new LeaveGroupCommand(_groupId), CancellationToken.None);

        group.Members.Should().NotContain(m => m.UserId == _userId);
        await _groups.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LeaderTriesToLeave_Throws()
    {
        var group = CreateGroupWithMembers(_userId);
        _currentUser.GetCurrentUserId().Returns(_leaderId);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        var act = async () => await CreateHandler().Handle(
            new LeaveGroupCommand(_groupId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*leader*");
    }

    [Fact]
    public async Task Handle_NotMember_Throws()
    {
        var group = CreateGroupWithMembers();
        _currentUser.GetCurrentUserId().Returns(_userId);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        var act = async () => await CreateHandler().Handle(
            new LeaveGroupCommand(_groupId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*not a member*");
    }
}
