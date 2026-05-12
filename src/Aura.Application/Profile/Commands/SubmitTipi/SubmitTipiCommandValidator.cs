using FluentValidation;

namespace Aura.Application.Profile.Commands.SubmitTipi;

public class SubmitTipiCommandValidator : AbstractValidator<SubmitTipiCommand>
{
    public SubmitTipiCommandValidator()
    {
        RuleFor(x => x.Answers)
            .NotNull().WithMessage("Answers are required.")
            .Must(a => a != null && a.Length == 10).WithMessage("Exactly 10 answers required.")
            .Must(a => a == null || a.All(v => v >= 1 && v <= 7)).WithMessage("Each answer must be between 1 and 7.");
    }
}
