using FluentValidation;

namespace Aura.Application.DormPreferences.Commands.SubmitPreferences;

public class SubmitPreferencesCommandValidator : AbstractValidator<SubmitPreferencesCommand>
{
    public SubmitPreferencesCommandValidator()
    {
        RuleFor(x => x.AllocationPeriodId)
            .NotEmpty().WithMessage("Allocation period ID is required.");

        RuleFor(x => x.DormitoryIds)
            .NotEmpty().WithMessage("At least one dormitory preference is required.");

        RuleFor(x => x.DormitoryIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Dormitory preferences must not contain duplicates.");
    }
}
