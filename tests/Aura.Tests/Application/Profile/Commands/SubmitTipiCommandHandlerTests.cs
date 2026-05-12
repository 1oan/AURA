using Aura.Application.Common.Interfaces;
using Aura.Application.Profile.Commands.SubmitTipi;
using Aura.Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.Profile.Commands;

public class SubmitTipiCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IStudentProfileRepository _profiles = Substitute.For<IStudentProfileRepository>();
    private readonly IStudentEmbeddingRepository _embeddings = Substitute.For<IStudentEmbeddingRepository>();
    private readonly Guid _userId = Guid.NewGuid();

    private SubmitTipiCommandHandler Create() => new(_currentUser, _profiles, _embeddings);

    [Fact]
    public async Task Handle_ValidAnswers_StoresRawAndComputedScores()
    {
        var existing = StudentProfile.Create(_userId);
        _currentUser.GetCurrentUserId().Returns(_userId);
        _profiles.FindByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(existing);

        var answers = new[] { 7, 2, 6, 2, 5, 2, 5, 3, 6, 3 };
        await Create().Handle(new SubmitTipiCommand(answers), CancellationToken.None);

        existing.TipiAnswers.Should().Equal(answers);
        existing.TipiExtraversion.Should().Be(6.5m);
        await _profiles.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoExistingProfile_CreatesProfileAndEmbedding()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _profiles.FindByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((StudentProfile?)null);

        await Create().Handle(new SubmitTipiCommand(new[] { 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 }), CancellationToken.None);

        await _profiles.Received(1).AddAsync(Arg.Any<StudentProfile>(), Arg.Any<CancellationToken>());
        await _embeddings.Received(1).AddAsync(Arg.Any<StudentEmbedding>(), Arg.Any<CancellationToken>());
    }
}
