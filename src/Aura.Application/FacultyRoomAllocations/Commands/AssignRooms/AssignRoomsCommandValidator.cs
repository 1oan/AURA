using FluentValidation;

namespace Aura.Application.FacultyRoomAllocations.Commands.AssignRooms;

public class AssignRoomsCommandValidator : AbstractValidator<AssignRoomsCommand>
{
    public AssignRoomsCommandValidator()
    {
        RuleFor(x => x.FacultyId).NotEmpty();
        RuleFor(x => x.AllocationPeriodId).NotEmpty();
        RuleFor(x => x.RoomIds).NotEmpty();
    }
}
