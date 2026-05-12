using Aura.Application.Common.Interfaces;
using Aura.Application.RoommateGroups.Common;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.RoommateGroups.Queries.GetCompatibleSuggestions;

public record GetCompatibleSuggestionsQuery(Guid GroupId) : IRequest<List<CompatibilityCandidateDto>>;

public class GetCompatibleSuggestionsQueryHandler(
    ICurrentUserService currentUser,
    IRoommateGroupRepository groupRepository,
    IUserRepository userRepository,
    ICompatibilityScorer scorer) : IRequestHandler<GetCompatibleSuggestionsQuery, List<CompatibilityCandidateDto>>
{
    public async Task<List<CompatibilityCandidateDto>> Handle(GetCompatibleSuggestionsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();
        var group = await groupRepository.FindByIdAsync(request.GroupId, cancellationToken)
            ?? throw new DomainException("Group not found.");
        if (group.Members.All(m => m.UserId != userId))
            throw new DomainException("You are not a member of this group.");

        var caller = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Current user not found.");

        // Build candidate pool: Accepted in same dorm, same gender, excluding group members
        var candidates = await userRepository.SearchByNameForLobbyAsync(
            string.Empty,
            group.AllocationPeriodId,
            group.DormitoryId,
            caller.Gender!.Value,
            excludeUserId: userId,
            limit: 100,
            cancellationToken);

        var memberIds = group.Members.Select(m => m.UserId).ToHashSet();
        var candidateIds = candidates
            .Where(c => !memberIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToList();

        // v1: NullCompatibilityScorer returns empty list
        var scores = await scorer.ScoreCandidatesAsync(userId, candidateIds, group.AllocationPeriodId, cancellationToken);

        var candidatesById = candidates.ToDictionary(c => c.Id);
        return scores
            .Where(s => candidatesById.ContainsKey(s.CandidateUserId))
            .OrderByDescending(s => s.Score)
            .Take(10)
            .Select(s =>
            {
                var c = candidatesById[s.CandidateUserId];
                return new CompatibilityCandidateDto(c.Id, c.FirstName, c.LastName, s.Score, s.SignalsUsed);
            })
            .ToList();
    }
}
