using FluentValidation;

namespace Aura.Application.FacultyRoomAllocations.Commands.RemoveRoomAssignments;

public class RemoveRoomAssignmentsCommandValidator : AbstractValidator<RemoveRoomAssignmentsCommand>
{
    public RemoveRoomAssignmentsCommandValidator()
    {
        RuleFor(x => x.FacultyId).NotEmpty();
        RuleFor(x => x.AllocationPeriodId).NotEmpty();
        RuleFor(x => x.RoomIds).NotEmpty();
    }
}
