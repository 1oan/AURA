using FluentValidation;

namespace Aura.Application.AllocationPeriods.Commands.CreateAllocationPeriod;

public class CreateAllocationPeriodCommandValidator : AbstractValidator<CreateAllocationPeriodCommand>
{
    public CreateAllocationPeriodCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Allocation period name is required.")
            .MaximumLength(200).WithMessage("Allocation period name must not exceed 200 characters.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");
    }
}
