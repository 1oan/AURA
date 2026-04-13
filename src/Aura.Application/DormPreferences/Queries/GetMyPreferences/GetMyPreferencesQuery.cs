using Aura.Application.Common.Interfaces;
using Aura.Application.DormPreferences.Common;
using MediatR;

namespace Aura.Application.DormPreferences.Queries.GetMyPreferences;

public record GetMyPreferencesQuery(Guid AllocationPeriodId) : IRequest<List<DormPreferenceDto>>;

public class GetMyPreferencesQueryHandler(
    ICurrentUserService currentUserService,
    IDormPreferenceRepository dormPreferenceRepository) : IRequestHandler<GetMyPreferencesQuery, List<DormPreferenceDto>>
{
    public async Task<List<DormPreferenceDto>> Handle(GetMyPreferencesQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();

        var preferences = await dormPreferenceRepository
            .GetByUserAndPeriodAsync(userId, query.AllocationPeriodId, cancellationToken);

        return preferences.Select(p => new DormPreferenceDto(
            p.DormitoryId,
            p.Dormitory!.Name,
            p.Dormitory.Campus.Name,
            p.Rank)).ToList();
    }
}
