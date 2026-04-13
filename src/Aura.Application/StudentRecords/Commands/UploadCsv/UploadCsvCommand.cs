using Aura.Application.Common.Interfaces;
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
        var records = new List<Domain.Entities.StudentRecord>();
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(request.CsvStream);
        var rowNumber = 0;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            rowNumber++;

            // Skip header row
            if (rowNumber == 1)
                continue;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var columns = line.Split(',');

            if (columns.Length < 5)
            {
                errors.Add(new CsvRowError(rowNumber, "Expected 5 columns: FirstName, LastName, MatriculationCode, Points, Gender."));
                continue;
            }

            var firstName = columns[0].Trim();
            var lastName = columns[1].Trim();
            var matriculationCode = columns[2].Trim();
            var pointsRaw = columns[3].Trim();
            var genderRaw = columns[4].Trim();

            if (string.IsNullOrWhiteSpace(firstName))
            {
                errors.Add(new CsvRowError(rowNumber, "First name is required."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(lastName))
            {
                errors.Add(new CsvRowError(rowNumber, "Last name is required."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(matriculationCode))
            {
                errors.Add(new CsvRowError(rowNumber, "Matriculation code is required."));
                continue;
            }

            if (!int.TryParse(pointsRaw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var points))
            {
                errors.Add(new CsvRowError(rowNumber, $"Invalid points value: '{pointsRaw}'."));
                continue;
            }

            if (points < 0)
            {
                errors.Add(new CsvRowError(rowNumber, "Points must be greater than or equal to zero."));
                continue;
            }

            if (!Enum.TryParse<Domain.Enums.Gender>(genderRaw, ignoreCase: true, out var gender))
            {
                errors.Add(new CsvRowError(rowNumber, $"Invalid gender value: '{genderRaw}'. Expected 'Male' or 'Female'."));
                continue;
            }

            if (!seenCodes.Add(matriculationCode))
            {
                errors.Add(new CsvRowError(rowNumber, $"Duplicate matriculation code: '{matriculationCode}'."));
                continue;
            }

            records.Add(Domain.Entities.StudentRecord.Create(
                matriculationCode, firstName, lastName, points, gender, facultyId, request.AllocationPeriodId));
        }

        // Re-upload replaces existing records for this faculty+period
        await studentRecordRepository.DeleteByFacultyAndPeriodAsync(facultyId, request.AllocationPeriodId, cancellationToken);
        await studentRecordRepository.AddRangeAsync(records, cancellationToken);
        await studentRecordRepository.SaveChangesAsync(cancellationToken);

        return new UploadCsvResult(records.Count, errors);
    }
}
