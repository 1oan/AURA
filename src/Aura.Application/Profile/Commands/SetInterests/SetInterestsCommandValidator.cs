using FluentValidation;

namespace Aura.Application.Profile.Commands.SetInterests;

public class SetInterestsCommandValidator : AbstractValidator<SetInterestsCommand>
{
    public SetInterestsCommandValidator()
    {
        RuleFor(x => x.Slugs)
            .NotNull()
            .Must(s => s == null || s.Length <= 50).WithMessage("Maximum of 50 interests.")
            .Must(s => s == null || s.Distinct().Count() == s.Length).WithMessage("Interest slugs cannot contain duplicates.")
            .Must(s => s == null || s.All(slug => !string.IsNullOrWhiteSpace(slug))).WithMessage("Slugs cannot be blank.");
    }
}
