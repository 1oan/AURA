namespace Aura.Application.Common.Interfaces;

// AI seam: v1 = NullCompatibilityScorer (returns empty list).
// KAN-86 Spec 4 swaps the registration to MultiSignalCompatibilityScorer in Aura.Infrastructure
// without touching any call site, DTO, or frontend.
public interface ICompatibilityScorer
{
    Task<List<CompatibilityScoreDto>> ScoreCandidatesAsync(
        Guid userId,
        IReadOnlyCollection<Guid> candidateUserIds,
        Guid periodId,
        CancellationToken cancellationToken);
}

public record CompatibilityScoreDto(
    Guid CandidateUserId,
    decimal Score,
    decimal LifestyleContribution,
    decimal? PersonalityContribution,
    decimal? SpotifyContribution,
    decimal? InterestsContribution,
    string[] SignalsUsed);
