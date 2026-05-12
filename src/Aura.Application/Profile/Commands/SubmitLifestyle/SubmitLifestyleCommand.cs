using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using MediatR;

namespace Aura.Application.Profile.Commands.SubmitLifestyle;

public record SubmitLifestyleCommand(
    SleepSchedule SleepSchedule,
    WakeUpTime WakeUpTime,
    int CleanlinessLevel,
    NoiseTolerance NoiseTolerance,
    StudyLocation StudyLocation,
    GuestFrequency GuestFrequency,
    SmokingHabit SmokingHabit) : IRequest;

public class SubmitLifestyleCommandHandler(
    ICurrentUserService currentUser,
    IStudentProfileRepository profileRepository,
    IStudentEmbeddingRepository embeddingRepository) : IRequestHandler<SubmitLifestyleCommand>
{
    public async Task Handle(SubmitLifestyleCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        var profile = await profileRepository.FindByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            profile = StudentProfile.Create(userId);
            await profileRepository.AddAsync(profile, cancellationToken);
            await embeddingRepository.AddAsync(StudentEmbedding.Create(userId), cancellationToken);
        }

        profile.SubmitLifestyle(
            request.SleepSchedule, request.WakeUpTime, request.CleanlinessLevel,
            request.NoiseTolerance, request.StudyLocation,
            request.GuestFrequency, request.SmokingHabit);

        await profileRepository.SaveChangesAsync(cancellationToken);
    }
}
