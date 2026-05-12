using Aura.Application.Common.Interfaces;
using MediatR;

namespace Aura.Application.Profile.Commands.DisconnectSpotify;

public record DisconnectSpotifyCommand : IRequest;

public class DisconnectSpotifyCommandHandler(
    ICurrentUserService currentUser,
    IStudentProfileRepository profileRepository) : IRequestHandler<DisconnectSpotifyCommand>
{
    public async Task Handle(DisconnectSpotifyCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        var profile = await profileRepository.FindByUserIdAsync(userId, cancellationToken);
        if (profile is null) return;

        profile.DisconnectSpotify();
        await profileRepository.SaveChangesAsync(cancellationToken);
    }
}
