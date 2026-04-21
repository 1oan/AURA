using System.Globalization;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.StudentRecords.Commands.UploadCsv;

public record UploadCsvCommand(Guid AllocationPeriodId, Stream CsvStream) : IRequest<UploadCsvResult>;

public record UploadCsvResult(int Created, List<CsvRowError> Errors);

public record CsvRowError(int Row, string Message);

public class UploadCsvCommandHandler(
    IStudentRecordRepository studentRecordRepository,
    IAllocationPeriodRepository allocationPeriodRepository,
    IUserRepository userRepository,
    ICurrentUserService currentUserService) : IRequestHandler<UploadCsvCommand, UploadCsvResult>
{
    public async Task<UploadCsvResult> Handle(UploadCsvCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.FacultyId is null)
            throw new DomainException("You must be assigned to a faculty to upload student records.");

        var period = await allocationPeriodRepository.FindByIdAsync(request.AllocationPeriodId, cancellationToken)
            ?? throw new NotFoundException("Allocation period not found.");

        if (period.Status != AllocationPeriodStatus.Draft)
            throw new DomainException("Student records can only be uploaded while the allocation period is in Draft status.");

        var facultyId = user.FacultyId.Value;
        var errors = new List<CsvRowError>();
        var records = new List<StudentRecord>();
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(request.CsvStream);
        var rowNumber = 0;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            rowNumber++;
            if (rowNumber == 1 || string.IsNullOrWhiteSpace(line))
                continue;

            var (record, error) = ParseRow(rowNumber, line, facultyId, request.AllocationPeriodId, seenCodes);
            if (error is not null) errors.Add(error);
            else if (record is not null) records.Add(record);
        }

        await studentRecordRepository.DeleteByFacultyAndPeriodAsync(facultyId, request.AllocationPeriodId, cancellationToken);
        await studentRecordRepository.AddRangeAsync(records, cancellationToken);
        await studentRecordRepository.SaveChangesAsync(cancellationToken);

        return new UploadCsvResult(records.Count, errors);
    }

    private static (StudentRecord? Record, CsvRowError? Error) ParseRow(
        int rowNumber, string line, Guid facultyId, Guid periodId, HashSet<string> seenCodes)
    {
        var columns = line.Split(',');
        if (columns.Length < 5)
            return (null, new CsvRowError(rowNumber, "Expected 5 columns: FirstName, LastName, MatriculationCode, Points, Gender."));

        var firstName = columns[0].Trim();
        var lastName = columns[1].Trim();
        var matriculationCode = columns[2].Trim();
        var pointsRaw = columns[3].Trim();
        var genderRaw = columns[4].Trim();

        if (string.IsNullOrWhiteSpace(firstName))
            return (null, new CsvRowError(rowNumber, "First name is required."));

        if (string.IsNullOrWhiteSpace(lastName))
            return (null, new CsvRowError(rowNumber, "Last name is required."));

        if (string.IsNullOrWhiteSpace(matriculationCode))
            return (null, new CsvRowError(rowNumber, "Matriculation code is required."));

        if (!int.TryParse(pointsRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var points))
            return (null, new CsvRowError(rowNumber, $"Invalid points value: '{pointsRaw}'."));

        if (points < 0)
            return (null, new CsvRowError(rowNumber, "Points must be greater than or equal to zero."));

        if (!Enum.TryParse<Gender>(genderRaw, ignoreCase: true, out var gender))
            return (null, new CsvRowError(rowNumber, $"Invalid gender value: '{genderRaw}'. Expected 'Male' or 'Female'."));

        if (!seenCodes.Add(matriculationCode))
            return (null, new CsvRowError(rowNumber, $"Duplicate matriculation code: '{matriculationCode}'."));

        var record = StudentRecord.Create(
            matriculationCode, firstName, lastName, points, gender, facultyId, periodId);
        return (record, null);
    }
}
