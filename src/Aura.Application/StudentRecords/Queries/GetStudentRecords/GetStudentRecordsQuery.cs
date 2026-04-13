using Aura.Application.Common.Interfaces;
using Aura.Application.StudentRecords.Common;
using MediatR;

namespace Aura.Application.StudentRecords.Queries.GetStudentRecords;

public record GetStudentRecordsQuery(Guid AllocationPeriodId, Guid? FacultyId) : IRequest<List<StudentRecordDto>>;

public class GetStudentRecordsQueryHandler(
    IStudentRecordRepository studentRecordRepository) : IRequestHandler<GetStudentRecordsQuery, List<StudentRecordDto>>
{
    public async Task<List<StudentRecordDto>> Handle(GetStudentRecordsQuery request, CancellationToken cancellationToken)
    {
        var records = await studentRecordRepository.GetByPeriodAndFacultyAsync(
            request.AllocationPeriodId, request.FacultyId, cancellationToken);

        return records
            .Select(r => new StudentRecordDto(
                r.Id,
                r.MatriculationCode,
                r.FirstName,
                r.LastName,
                r.Points,
                r.Gender.ToString(),
                r.FacultyId,
                r.Faculty!.Name,
                r.Faculty.Abbreviation,
                r.AllocationPeriodId,
                r.UserId))
            .ToList();
    }
}
