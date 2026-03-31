using FluentValidation;

namespace Aura.Application.StudentRecords.Commands.UploadCsv;

public class UploadCsvCommandValidator : AbstractValidator<UploadCsvCommand>
{
    public UploadCsvCommandValidator()
    {
        RuleFor(x => x.AllocationPeriodId)
            .NotEmpty().WithMessage("Allocation period ID is required.");

        RuleFor(x => x.CsvStream)
            .NotNull().WithMessage("CSV file is required.");
    }
}
