using Aura.Application.Common.Interfaces;
using Aura.Application.Profile.Commands.SubmitLifestyle;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using FluentAssertions;
using NSubstitute;

namespace Aura.Tests.Application.Profile.Commands;

public class SubmitLifestyleCommandHandlerTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IStudentProfileRepository _profiles = Substitute.For<IStudentProfileRepository>();
    private readonly IStudentEmbeddingRepository _embeddings = Substitute.For<IStudentEmbeddingRepository>();
    private readonly Guid _userId = Guid.NewGuid();

    private SubmitLifestyleCommandHandler Create() => new(_currentUser, _profiles, _embeddings);

    private static SubmitLifestyleCommand ValidCommand() => new(
        SleepSchedule.Late, WakeUpTime.Late, 4,
        NoiseTolerance.Some, StudyLocation.Mixed,
        GuestFrequency.Weekly, SmokingHabit.OutdoorsOnly);

    [Fact]
    public async Task Handle_NoExistingProfile_CreatesProfileAndEmbeddingThenSavesLifestyle()
    {
        _currentUser.GetCurrentUserId().Returns(_userId);
        _profiles.FindByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns((StudentProfile?)null);

        await Create().Handle(ValidCommand(), CancellationToken.None);

        await _profiles.Received(1).AddAsync(
            Arg.Is<StudentProfile>(p => p.UserId == _userId && p.LifestyleCompletedAt != null),
            Arg.Any<CancellationToken>());
        await _embeddings.Received(1).AddAsync(
            Arg.Is<StudentEmbedding>(e => e.UserId == _userId),
            Arg.Any<CancellationToken>());
        await _profiles.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingProfile_UpdatesInPlaceWithoutAddingDuplicateEmbedding()
    {
        var existing = StudentProfile.Create(_userId);
        _currentUser.GetCurrentUserId().Returns(_userId);
        _profiles.FindByUserIdAsync(_userId, Arg.Any<CancellationToken>()).Returns(existing);

        await Create().Handle(ValidCommand(), CancellationToken.None);

        existing.LifestyleCompletedAt.Should().NotBeNull();
        await _profiles.DidNotReceive().AddAsync(Arg.Any<StudentProfile>(), Arg.Any<CancellationToken>());
        await _embeddings.DidNotReceive().AddAsync(Arg.Any<StudentEmbedding>(), Arg.Any<CancellationToken>());
        await _profiles.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
