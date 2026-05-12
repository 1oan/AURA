using Aura.Application.Common.Interfaces;
using Aura.Application.Profile.Commands.SetInterests;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.Profile.Commands;

public class SetInterestsCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IStudentProfileRepository _profiles = Substitute.For<IStudentProfileRepository>();
    private readonly IStudentEmbeddingRepository _embeddings = Substitute.For<IStudentEmbeddingRepository>();
    private readonly IInterestRepository _interests = Substitute.For<IInterestRepository>();
    private readonly Guid _userId = Guid.NewGuid();

    private SetInterestsCommandHandler Create() =>
        new(_currentUser, _profiles, _embeddings, _interests);

    [Fact]
    public async Task Handle_AllSlugsInActiveCatalog_SavesInterests()
    {
        var existing = StudentProfile.Create(_userId);
        _currentUser.GetCurrentUserId().Returns(_userId);
        _profiles.FindByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(existing);
        _interests.GetActiveBySlugsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Interest>
            {
                Interest.Create(Guid.NewGuid(), "football", "Football", "sports", 1),
                Interest.Create(Guid.NewGuid(), "gaming", "Gaming", "entertainment", 4),
            });

        await Create().Handle(new SetInterestsCommand(new[] { "football", "gaming" }), CancellationToken.None);

        existing.InterestSlugs.Should().Equal("football", "gaming");
        await _profiles.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SlugNotInCatalog_Throws()
    {
        var existing = StudentProfile.Create(_userId);
        _currentUser.GetCurrentUserId().Returns(_userId);
        _profiles.FindByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(existing);
        _interests.GetActiveBySlugsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Interest>
            {
                Interest.Create(Guid.NewGuid(), "football", "Football", "sports", 1),
                // "ghost-slug" not returned
            });

        var act = async () => await Create().Handle(
            new SetInterestsCommand(new[] { "football", "ghost-slug" }), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*ghost-slug*");
    }

    [Fact]
    public async Task Handle_EmptyArray_AllowedAndStampsTimestamp()
    {
        var existing = StudentProfile.Create(_userId);
        _currentUser.GetCurrentUserId().Returns(_userId);
        _profiles.FindByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(existing);
        _interests.GetActiveBySlugsAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Interest>());

        await Create().Handle(new SetInterestsCommand(Array.Empty<string>()), CancellationToken.None);

        existing.InterestSlugs.Should().BeEmpty();
        existing.InterestsCompletedAt.Should().NotBeNull();
    }
}
