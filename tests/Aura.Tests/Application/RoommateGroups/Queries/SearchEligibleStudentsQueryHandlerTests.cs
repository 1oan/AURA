using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Queries.SearchEligibleStudents;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Queries;

public class SearchEligibleStudentsQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IDormAllocationRepository _allocations = Substitute.For<IDormAllocationRepository>();
    private readonly IRoommateGroupRepository _groups = Substitute.For<IRoommateGroupRepository>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();

    private SearchEligibleStudentsQueryHandler CreateHandler() =>
        new(_currentUser, _users, _allocations, _groups);

    private User MakeCaller()
    {
        var user = User.Create("caller@uaic.ro", "hash");
        user.SetPrivateProperty("Id", _userId);
        user.SetGender(Gender.Female);
        return user;
    }

    private DormAllocation MakeAcceptedAllocation()
    {
        var alloc = DormAllocation.Create(_userId, _dormId, _periodId, 1);
        alloc.Accept();
        return alloc;
    }

    [Fact]
    public async Task Handle_QueryTooShort_ReturnsEmpty()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);

        var result = await CreateHandler().Handle(new SearchEligibleStudentsQuery("a", _periodId), CancellationToken.None);

        result.Should().BeEmpty();
        await _users.DidNotReceive().SearchByNameForLobbyAsync(
            Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<Gender>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoAllocation_ThrowsDomainException()
    {
        var caller = MakeCaller();
        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(caller);
        _allocations.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>()).Returns((DormAllocation?)null);

        var act = () => CreateHandler().Handle(new SearchEligibleStudentsQuery("Ana", _periodId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Handle_MatchAlreadyInGroup_IsFiltered()
    {
        var caller = MakeCaller();
        var alloc = MakeAcceptedAllocation();

        var candidate = User.Create("cand@uaic.ro", "hash");
        var candidateId = Guid.NewGuid();
        candidate.SetPrivateProperty("Id", candidateId);
        candidate.UpdateProfile("Ana", "Maria");
        candidate.SetMatriculationCode("1234");

        var existingGroup = RoommateGroup.Create(_periodId, _dormId, candidateId, RoomSizePreference.TwoBed);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(caller);
        _allocations.FindActiveByUserAndPeriodAsync(_userId, _periodId, Arg.Any<CancellationToken>()).Returns(alloc);
        _users.SearchByNameForLobbyAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<Gender>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<User> { candidate });
        _groups.FindActiveByUserAndPeriodAsync(candidateId, _periodId, Arg.Any<CancellationToken>()).Returns(existingGroup);

        var result = await CreateHandler().Handle(new SearchEligibleStudentsQuery("Ana", _periodId), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
