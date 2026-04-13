namespace Aura.Application.StudentRecords.Common;

public record StudentRecordDto(
    Guid Id,
    string MatriculationCode,
    string FirstName,
    string LastName,
    int Points,
    string Gender,
    Guid FacultyId,
    string FacultyName,
    string FacultyAbbreviation,
    Guid AllocationPeriodId,
    Guid? UserId);
