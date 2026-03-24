using FluentValidation;

namespace Aura.Application.Faculties.Commands.UpdateFaculty;

public class UpdateFacultyCommandValidator : AbstractValidator<UpdateFacultyCommand>
{
    public UpdateFacultyCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Faculty id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Faculty name is required.")
            .MaximumLength(200).WithMessage("Faculty name must not exceed 200 characters.");

        RuleFor(x => x.Abbreviation)
            .NotEmpty().WithMessage("Faculty abbreviation is required.")
            .MaximumLength(20).WithMessage("Faculty abbreviation must not exceed 20 characters.");
    }
}
