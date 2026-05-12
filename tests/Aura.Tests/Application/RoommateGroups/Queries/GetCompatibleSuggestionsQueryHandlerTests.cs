using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Queries.GetCompatibleSuggestions;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using Aura.Tests.Support;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.RoommateGroups.Queries;

public class GetCompatibleSuggestionsQueryHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IRoommateGroupRepository _groups = Substitute.For<IRoommateGroupRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICompatibilityScorer _scorer = Substitute.For<ICompatibilityScorer>();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _groupId = Guid.NewGuid();
    private readonly Guid _periodId = Guid.NewGuid();
    private readonly Guid _dormId = Guid.NewGuid();

    private GetCompatibleSuggestionsQueryHandler CreateHandler() =>
        new(_currentUser, _groups, _users, _scorer);

    [Fact]
    public async Task Handle_GroupNotFound_ThrowsDomainException()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns((RoommateGroup?)null);

        var act = () => CreateHandler().Handle(new GetCompatibleSuggestionsQuery(_groupId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Group not found*");
    }

    [Fact]
    public async Task Handle_UserNotInGroup_ThrowsDomainException()
    {
        var otherUserId = Guid.NewGuid();
        var group = RoommateGroup.Create(_periodId, _dormId, otherUserId, RoomSizePreference.TwoBed);
        group.SetPrivateProperty("Id", _groupId);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);

        var act = () => CreateHandler().Handle(new GetCompatibleSuggestionsQuery(_groupId), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*not a member*");
    }

    [Fact]
    public async Task Handle_NullScorerReturnsEmpty_ReturnsEmptyList()
    {
        var group = RoommateGroup.Create(_periodId, _dormId, _userId, RoomSizePreference.TwoBed);
        group.SetPrivateProperty("Id", _groupId);

        var caller = User.Create("caller@uaic.ro", "hash");
        caller.SetPrivateProperty("Id", _userId);
        caller.SetGender(Gender.Female);

        _currentUser.GetCurrentUserId().Returns(_userId);
        _groups.FindByIdAsync(_groupId, Arg.Any<CancellationToken>()).Returns(group);
        _users.FindByIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(caller);
        _users.SearchByNameForLobbyAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<Guid>(),
            Arg.Any<Gender>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<User>());
        _scorer.ScoreCandidatesAsync(Arg.Any<Guid>(), Arg.Any<IReadOnlyCollection<Guid>>(),
            Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<CompatibilityScoreDto>());

        var result = await CreateHandler().Handle(new GetCompatibleSuggestionsQuery(_groupId), CancellationToken.None);

        result.Should().BeEmpty();
    }
}
