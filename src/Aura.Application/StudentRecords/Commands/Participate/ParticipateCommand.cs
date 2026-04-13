using System.Globalization;
using System.Text;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.StudentRecords.Commands.Participate;

public record ParticipateCommand(Guid AllocationPeriodId, string? MatriculationCode) : IRequest<ParticipateResult>;

public record ParticipateResult(string FacultyName, string FacultyAbbreviation, int Points);

public class ParticipateCommandHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IAllocationPeriodRepository allocationPeriodRepository,
    IStudentRecordRepository studentRecordRepository) : IRequestHandler<ParticipateCommand, ParticipateResult>
{
    public async Task<ParticipateResult> Handle(ParticipateCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        // Determine matriculation code: use provided one (and persist it), or fall back to existing
        var matriculationCode = command.MatriculationCode;
        if (!string.IsNullOrWhiteSpace(matriculationCode))
        {
            user.SetMatriculationCode(matriculationCode);
        }
        else
        {
            matriculationCode = user.MatriculationCode;
        }

        if (string.IsNullOrWhiteSpace(matriculationCode))
            throw new DomainException("Matriculation code is required.");

        var period = await allocationPeriodRepository.FindByIdAsync(command.AllocationPeriodId, cancellationToken)
            ?? throw new NotFoundException("Allocation period not found.");

        if (period.Status != AllocationPeriodStatus.Open)
            throw new DomainException("Allocation period is not open for participation.");

        var studentRecord = await studentRecordRepository.FindByMatriculationCodeAndPeriodAsync(
            matriculationCode, command.AllocationPeriodId, cancellationToken)
            ?? throw new NotFoundException("You are not eligible for this allocation period.");

        if (studentRecord.UserId is not null)
            throw new DomainException("You have already participated in this allocation period.");

        // Identity verification: email parts must match CSV names in either order
        var localPart = user.Email.Split('@')[0];
        var segments = localPart.Split('.');

        if (segments.Length >= 2)
        {
            var emailPart1 = NormalizeName(segments[0]);
            var emailPart2 = NormalizeName(segments[^1]);
            var recordFirst = NormalizeName(studentRecord.FirstName);
            var recordLast = NormalizeName(studentRecord.LastName);

            var normalOrder = emailPart1 == recordFirst && emailPart2 == recordLast;
            var reversedOrder = emailPart1 == recordLast && emailPart2 == recordFirst;

            if (!normalOrder && !reversedOrder)
                throw new DomainException(
                    "Identity mismatch \u2014 the name in your email does not match the student record. Contact your faculty admin.");
        }

        studentRecord.LinkToUser(user.Id);
        user.UpdateProfile(studentRecord.FirstName, studentRecord.LastName);
        user.AssignToFaculty(studentRecord.FacultyId);
        user.SetGender(studentRecord.Gender);

        await studentRecordRepository.SaveChangesAsync(cancellationToken);

        return new ParticipateResult(
            studentRecord.Faculty!.Name,
            studentRecord.Faculty.Abbreviation,
            studentRecord.Points);
    }

    private static string NormalizeName(string input) =>
        new string(input
            .Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .ToArray())
        .ToLowerInvariant()
        .Trim();
}
