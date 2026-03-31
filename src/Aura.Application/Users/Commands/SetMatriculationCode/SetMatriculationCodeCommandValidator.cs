using FluentValidation;

namespace Aura.Application.Users.Commands.SetMatriculationCode;

public class SetMatriculationCodeCommandValidator : AbstractValidator<SetMatriculationCodeCommand>
{
    public SetMatriculationCodeCommandValidator()
    {
        RuleFor(x => x.MatriculationCode)
            .NotEmpty().WithMessage("Matriculation code is required.")
            .MaximumLength(50).WithMessage("Matriculation code must not exceed 50 characters.");
    }
}
