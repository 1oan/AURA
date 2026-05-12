namespace Aura.Application.RoommateGroups.Common;

public record EligibleStudentDto(
    Guid UserId,
    string FirstName,
    string LastName,
    string MatriculationCode);
