using FluentValidation;

namespace Aura.Application.Campuses.Commands.CreateCampus;

public class CreateCampusCommandValidator : AbstractValidator<CreateCampusCommand>
{
    public CreateCampusCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Campus name is required.")
            .MaximumLength(200).WithMessage("Campus name must not exceed 200 characters.");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Campus address must not exceed 500 characters.")
            .When(x => x.Address is not null);
    }
}
