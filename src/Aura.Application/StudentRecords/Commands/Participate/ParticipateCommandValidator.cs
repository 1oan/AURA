using FluentValidation;

namespace Aura.Application.StudentRecords.Commands.Participate;

public class ParticipateCommandValidator : AbstractValidator<ParticipateCommand>
{
    public ParticipateCommandValidator()
    {
        RuleFor(x => x.AllocationPeriodId)
            .NotEmpty().WithMessage("Allocation period ID is required.");

        RuleFor(x => x.MatriculationCode)
            .MaximumLength(50).WithMessage("Matriculation code must not exceed 50 characters.")
            .When(x => x.MatriculationCode is not null);
    }
}
