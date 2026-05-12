using Aura.Application.Common.Interfaces;

namespace Aura.Application.Compatibility;

public class NullCompatibilityScorer : ICompatibilityScorer
{
    public Task<List<CompatibilityScoreDto>> ScoreCandidatesAsync(
        Guid userId,
        IReadOnlyCollection<Guid> candidateUserIds,
        Guid periodId,
        CancellationToken cancellationToken)
        => Task.FromResult(new List<CompatibilityScoreDto>());
}
