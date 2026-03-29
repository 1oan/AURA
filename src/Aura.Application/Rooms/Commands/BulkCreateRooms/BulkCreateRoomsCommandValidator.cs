using FluentValidation;

namespace Aura.Application.Rooms.Commands.BulkCreateRooms;

public class BulkCreateRoomsCommandValidator : AbstractValidator<BulkCreateRoomsCommand>
{
    public BulkCreateRoomsCommandValidator()
    {
        RuleFor(x => x.DormitoryId)
            .NotEmpty().WithMessage("Dormitory ID is required.");

        RuleFor(x => x.Floors)
            .NotEmpty().WithMessage("At least one floor configuration is required.");

        RuleForEach(x => x.Floors).ChildRules(floor =>
        {
            floor.RuleFor(f => f.FloorNumber)
                .GreaterThanOrEqualTo(0).WithMessage("Floor number must be zero or positive.");

            floor.RuleFor(f => f.RoomCount)
                .InclusiveBetween(1, 50).WithMessage("Room count per floor must be between 1 and 50.");

            floor.RuleFor(f => f.Capacity)
                .InclusiveBetween(1, 10).WithMessage("Room capacity must be between 1 and 10.");

            floor.RuleFor(f => f.Gender)
                .NotEmpty().WithMessage("Gender is required for each floor configuration.");
        });
    }
}
