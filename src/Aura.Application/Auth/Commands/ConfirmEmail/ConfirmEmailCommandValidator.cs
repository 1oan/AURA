using FluentValidation;

namespace Aura.Application.Auth.Commands.ConfirmEmail;

public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Confirmation code is required.")
            .Length(6).WithMessage("Confirmation code must be exactly 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("Confirmation code must contain only digits.");
    }
}