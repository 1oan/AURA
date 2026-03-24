using FluentValidation;

namespace Aura.Application.Dormitories.Commands.UpdateDormitory;

public class UpdateDormitoryCommandValidator : AbstractValidator<UpdateDormitoryCommand>
{
    public UpdateDormitoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Dormitory ID is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Dormitory name is required.")
            .MaximumLength(200).WithMessage("Dormitory name must not exceed 200 characters.");
    }
}
