using FluentValidation;

namespace Aura.Application.AllocationPeriods.Commands.UpdateAllocationPeriod;

public class UpdateAllocationPeriodCommandValidator : AbstractValidator<UpdateAllocationPeriodCommand>
{
    public UpdateAllocationPeriodCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Allocation period id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Allocation period name is required.")
            .MaximumLength(200).WithMessage("Allocation period name must not exceed 200 characters.");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");
    }
}
