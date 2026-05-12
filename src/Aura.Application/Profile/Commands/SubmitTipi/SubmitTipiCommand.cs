using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using MediatR;

namespace Aura.Application.Profile.Commands.SubmitTipi;

public record SubmitTipiCommand(int[] Answers) : IRequest;

public class SubmitTipiCommandHandler(
    ICurrentUserService currentUser,
    IStudentProfileRepository profileRepository,
    IStudentEmbeddingRepository embeddingRepository) : IRequestHandler<SubmitTipiCommand>
{
    public async Task Handle(SubmitTipiCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        var profile = await profileRepository.FindByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            profile = StudentProfile.Create(userId);
            await profileRepository.AddAsync(profile, cancellationToken);
            await embeddingRepository.AddAsync(StudentEmbedding.Create(userId), cancellationToken);
        }

        profile.SubmitTipi(request.Answers);
        await profileRepository.SaveChangesAsync(cancellationToken);
    }
}
