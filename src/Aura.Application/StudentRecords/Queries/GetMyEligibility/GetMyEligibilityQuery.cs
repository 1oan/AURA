using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.StudentRecords.Queries.GetMyEligibility;

public record GetMyEligibilityQuery(Guid AllocationPeriodId) : IRequest<MyEligibilityResult>;

public record MyEligibilityResult(
    bool IsEligible,
    bool HasParticipated,
    string? FacultyName,
    string? FacultyAbbreviation,
    int? Points,
    string? MatriculationCode);

public class GetMyEligibilityQueryHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IStudentRecordRepository studentRecordRepository) : IRequestHandler<GetMyEligibilityQuery, MyEligibilityResult>
{
    public async Task<MyEligibilityResult> Handle(GetMyEligibilityQuery query, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (string.IsNullOrWhiteSpace(user.MatriculationCode))
            return new MyEligibilityResult(false, false, null, null, null, null);

        var studentRecord = await studentRecordRepository.FindByMatriculationCodeAndPeriodAsync(
            user.MatriculationCode, query.AllocationPeriodId, cancellationToken);

        if (studentRecord is null)
            return new MyEligibilityResult(false, false, null, null, null, user.MatriculationCode);

        if (studentRecord.UserId == userId)
        {
            return new MyEligibilityResult(
                true,
                true,
                studentRecord.Faculty!.Name,
                studentRecord.Faculty.Abbreviation,
                studentRecord.Points,
                user.MatriculationCode);
        }

        // Someone else already linked to this record
        if (studentRecord.UserId is not null)
            return new MyEligibilityResult(false, false, null, null, null, user.MatriculationCode);

        return new MyEligibilityResult(
            true,
            false,
            studentRecord.Faculty!.Name,
            studentRecord.Faculty.Abbreviation,
            studentRecord.Points,
            user.MatriculationCode);
    }
}
