using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Commands.InviteToGroup;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using MediatR;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Commands;

public class InviteToGroupCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IRoommateGroupRepository _groups = Substitute.For<IRoommateGroupRepository>();
    private readonly IGroupInvitationRepository _invitations = Substitute.For<IGroupInvitationRepository>();
    private readonly IDormAllocationRepository _allocations = Substitute.For<IDormAllocationRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();

    private readonly Guid _leaderId = Guid.NewGuid();
    private readonly Guid _inviteeId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();
    private readonly Guid _otherDormId = Guid.NewGuid();
    private readonly Guid _groupId = Guid.NewGuid();

    private InviteToGroupCommandHandler CreateHandler() =>
        new(_currentUser, _groups, _invitations, _allocations, _users, _publisher);

    private RoommateGroup CreateGroup(RoomSizePreference pref = RoomSizePreference.ThreeBed)
    {
        var group = RoommateGroup.Create(_periodId, _dormId, _leaderId, pref);
        group.SetPrivateProperty("Id", _groupId);
        return group;
    }

    private User CreateUser(Guid id, Gender gender = Gender.Male)
    {
        var user = User.Create($"u-{id}@uaic.ro", "hash");
        user.SetPrivateProperty("Id", id);
        user.SetGender(gender);
        return user;
    }

    private DormAllocation CreateAcceptedAllocation(Guid userId, Guid dormId)
    {
        var alloc = DormAllocation.Create(userId, dormId, _periodId, 1);
        alloc.Accept();
        return alloc;
    }

    private void SetupHappyPath(Gender inviteeGender = Gender.Male)
    {
        _currentUser.GetCurrentUserId().Returns(_leaderId);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(CreateGroup());
        _users.FindByIdAsync(_leaderId, Arg.Any<CancellationToken>()).Returns(CreateUser(_leaderId, Gender.Male));
        _users.FindByIdAsync(_inviteeId, Arg.Any<CancellationToken>()).Returns(CreateUser(_inviteeId, inviteeGender));
        _allocations.FindActiveByUserAndPeriodAsync(_inviteeId, _periodId, Arg.Any<CancellationToken>())
            .Returns(CreateAcceptedAllocation(_inviteeId, _dormId));
        _groups.FindActiveByUserAndPeriodAsync(_inviteeId, _periodId, Arg.Any<CancellationToken>())
            .Returns((RoommateGroup?)null);
        _invitations.FindPendingAsync(_groupId, _inviteeId, Arg.Any<CancellationToken>())
            .Returns((GroupInvitation?)null);
    }

    [Fact]
    public async Task Handle_HappyPath_CreatesInvitationAndPublishesEvent()
    {
        SetupHappyPath();

        var result = await CreateHandler().Handle(
            new InviteToGroupCommand(_groupId, _inviteeId), CancellationToken.None);

        await _invitations.Received(1).AddAsync(
            Arg.Is<GroupInvitation>(i =>
                i.GroupId == _groupId
                && i.InviterUserId == _leaderId
                && i.InviteeUserId == _inviteeId),
            Arg.Any<CancellationToken>());
        await _invitations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _publisher.Received(1).Publish(
            Arg.Is<GroupInvitationCreatedEvent>(e => e.InviteeUserId == _inviteeId),
            Arg.Any<CancellationToken>());
        result.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_NotLeader_Throws()
    {
        SetupHappyPath();
        _currentUser.GetCurrentUserId().Returns(Guid.NewGuid());

        var act = async () => await CreateHandler().Handle(
            new InviteToGroupCommand(_groupId, _inviteeId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*leader*");
    }

    [Fact]
    public async Task Handle_GroupNotForming_Throws()
    {
        SetupHappyPath();
        var group = CreateGroup(RoomSizePreference.TwoBed);
        group.AddMember(Guid.NewGuid());
        group.Lock();
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        var act = async () => await CreateHandler().Handle(
            new InviteToGroupCommand(_groupId, _inviteeId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Forming*");
    }

    [Fact]
    public async Task Handle_GroupAtCapacity_Throws()
    {
        SetupHappyPath();
        var group = CreateGroup(RoomSizePreference.TwoBed);
        group.AddMember(Guid.NewGuid());
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        var act = async () => await CreateHandler().Handle(
            new InviteToGroupCommand(_groupId, _inviteeId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*capacity*");
    }

    [Fact]
    public async Task Handle_InviteeAlreadyMember_Throws()
    {
        SetupHappyPath();
        var group = CreateGroup();
        group.AddMember(_inviteeId);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        var act = async () => await CreateHandler().Handle(
            new InviteToGroupCommand(_groupId, _inviteeId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*already a member*");
    }

    [Fact]
    public async Task Handle_GenderMismatch_Throws()
    {
        SetupHappyPath(inviteeGender: Gender.Female);

        var act = async () => await CreateHandler().Handle(
            new InviteToGroupCommand(_groupId, _inviteeId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*gender*");
    }

    [Fact]
    public async Task Handle_InviteeNoAcceptedAllocation_Throws()
    {
        SetupHappyPath();
        _allocations.FindActiveByUserAndPeriodAsync(_inviteeId, _periodId, Arg.Any<CancellationToken>())
            .Returns((DormAllocation?)null);

        var act = async () => await CreateHandler().Handle(
            new InviteToGroupCommand(_groupId, _inviteeId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*allocation*");
    }

    [Fact]
    public async Task Handle_InviteeDifferentDorm_Throws()
    {
        SetupHappyPath();
        _allocations.FindActiveByUserAndPeriodAsync(_inviteeId, _periodId, Arg.Any<CancellationToken>())
            .Returns(CreateAcceptedAllocation(_inviteeId, _otherDormId));

        var act = async () => await CreateHandler().Handle(
            new InviteToGroupCommand(_groupId, _inviteeId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*dorm*");
    }

    [Fact]
    public async Task Handle_DuplicatePendingInvitation_Throws()
    {
        SetupHappyPath();
        var pending = GroupInvitation.Create(_groupId, _leaderId, _inviteeId);
        _invitations.FindPendingAsync(_groupId, _inviteeId, Arg.Any<CancellationToken>()).Returns(pending);

        var act = async () => await CreateHandler().Handle(
            new InviteToGroupCommand(_groupId, _inviteeId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*pending invitation*");
    }
}
