using FluentValidation;

namespace Aura.Application.Rooms.Commands.UpdateRoom;

public class UpdateRoomCommandValidator : AbstractValidator<UpdateRoomCommand>
{
    public UpdateRoomCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Room ID is required.");

        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("Room number is required.")
            .MaximumLength(20).WithMessage("Room number must not exceed 20 characters.");

        RuleFor(x => x.Floor)
            .GreaterThanOrEqualTo(0).WithMessage("Floor must be zero or positive.");

        RuleFor(x => x.Capacity)
            .InclusiveBetween(1, 10).WithMessage("Room capacity must be between 1 and 10.");

        RuleFor(x => x.Gender)
            .NotEmpty().WithMessage("Gender is required.");
    }
}
