using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Profile.Commands.SetInterests;

public record SetInterestsCommand(string[] Slugs) : IRequest;

public class SetInterestsCommandHandler(
    ICurrentUserService currentUser,
    IStudentProfileRepository profileRepository,
    IStudentEmbeddingRepository embeddingRepository,
    IInterestRepository interestRepository) : IRequestHandler<SetInterestsCommand>
{
    public async Task Handle(SetInterestsCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();

        if (request.Slugs.Length > 0)
        {
            var active = await interestRepository.GetActiveBySlugsAsync(request.Slugs, cancellationToken);
            var activeSlugs = active.Select(i => i.Slug).ToHashSet();
            var missing = request.Slugs.Where(s => !activeSlugs.Contains(s)).ToList();
            if (missing.Count > 0)
                throw new DomainException(
                    $"Unknown or inactive interest slugs: {string.Join(", ", missing)}.");
        }

        var profile = await profileRepository.FindByUserIdAsync(userId, cancellationToken);
        if (profile is null)
        {
            profile = StudentProfile.Create(userId);
            await profileRepository.AddAsync(profile, cancellationToken);
            await embeddingRepository.AddAsync(StudentEmbedding.Create(userId), cancellationToken);
        }

        profile.SetInterests(request.Slugs);
        await profileRepository.SaveChangesAsync(cancellationToken);
    }
}
