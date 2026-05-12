using Aura.Application.Common.Interfaces;
using Aura.Application.Profile.Common;
using MediatR;

namespace Aura.Application.Profile.Queries.GetMyProfile;

public record GetMyProfileQuery : IRequest<ProfileDto>;

public class GetMyProfileQueryHandler(
    ICurrentUserService currentUser,
    IStudentProfileRepository profileRepository) : IRequestHandler<GetMyProfileQuery, ProfileDto>
{
    public async Task<ProfileDto> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        var profile = await profileRepository.FindByUserIdAsync(userId, cancellationToken);

        if (profile is null)
        {
            return new ProfileDto(
                CompletenessPercent: 0,
                HasLifestyleData: false,
                Lifestyle: null,
                Tipi: null,
                Interests: new InterestsDto(Array.Empty<string>(), null),
                Spotify: new SpotifyDto(false, null, Array.Empty<string>()));
        }

        var lifestyle = profile.LifestyleCompletedAt is null ? null : new LifestyleDto(
            profile.SleepSchedule!.Value.ToString(),
            profile.WakeUpTime!.Value.ToString(),
            profile.CleanlinessLevel!.Value,
            profile.NoiseTolerance!.Value.ToString(),
            profile.StudyLocation!.Value.ToString(),
            profile.GuestFrequency!.Value.ToString(),
            profile.SmokingHabit!.Value.ToString(),
            profile.LifestyleCompletedAt.Value);

        var tipi = profile.TipiCompletedAt is null ? null : new TipiDto(
            profile.TipiExtraversion!.Value,
            profile.TipiAgreeableness!.Value,
            profile.TipiConscientiousness!.Value,
            profile.TipiEmotionalStability!.Value,
            profile.TipiOpenness!.Value,
            profile.TipiCompletedAt.Value);

        return new ProfileDto(
            profile.CompletenessPercent,
            profile.HasLifestyleData,
            lifestyle,
            tipi,
            new InterestsDto(profile.InterestSlugs, profile.InterestsCompletedAt),
            new SpotifyDto(profile.SpotifyConnected, profile.SpotifyConnectedAt, profile.SpotifyScopes));
    }
}
