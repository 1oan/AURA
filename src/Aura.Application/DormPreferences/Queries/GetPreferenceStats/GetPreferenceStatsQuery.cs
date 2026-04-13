using Aura.Application.Common.Interfaces;
using Aura.Application.DormPreferences.Common;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.DormPreferences.Queries.GetPreferenceStats;

public record GetPreferenceStatsQuery(Guid AllocationPeriodId) : IRequest<PreferenceStatsDto>;

public class GetPreferenceStatsQueryHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IStudentRecordRepository studentRecordRepository,
    IDormPreferenceRepository dormPreferenceRepository) : IRequestHandler<GetPreferenceStatsQuery, PreferenceStatsDto>
{
    public async Task<PreferenceStatsDto> Handle(GetPreferenceStatsQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var facultyId = user.FacultyId
            ?? throw new DomainException("User is not assigned to a faculty.");

        var totalParticipants = await studentRecordRepository
            .CountParticipantsByPeriodAndFacultyAsync(query.AllocationPeriodId, facultyId, cancellationToken);

        var studentsWithPreferences = await dormPreferenceRepository
            .CountByPeriodAndFacultyAsync(query.AllocationPeriodId, facultyId, cancellationToken);

        return new PreferenceStatsDto(totalParticipants, studentsWithPreferences);
    }
}
