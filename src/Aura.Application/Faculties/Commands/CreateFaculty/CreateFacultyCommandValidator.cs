using FluentValidation;

namespace Aura.Application.Faculties.Commands.CreateFaculty;

public class CreateFacultyCommandValidator : AbstractValidator<CreateFacultyCommand>
{
    public CreateFacultyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Faculty name is required.")
            .MaximumLength(200).WithMessage("Faculty name must not exceed 200 characters.");

        RuleFor(x => x.Abbreviation)
            .NotEmpty().WithMessage("Faculty abbreviation is required.")
            .MaximumLength(20).WithMessage("Faculty abbreviation must not exceed 20 characters.");
    }
}
