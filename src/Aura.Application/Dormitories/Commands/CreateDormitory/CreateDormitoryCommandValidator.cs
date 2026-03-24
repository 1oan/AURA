using FluentValidation;

namespace Aura.Application.Dormitories.Commands.CreateDormitory;

public class CreateDormitoryCommandValidator : AbstractValidator<CreateDormitoryCommand>
{
    public CreateDormitoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Dormitory name is required.")
            .MaximumLength(200).WithMessage("Dormitory name must not exceed 200 characters.");

        RuleFor(x => x.CampusId)
            .NotEmpty().WithMessage("Campus ID is required.");
    }
}
