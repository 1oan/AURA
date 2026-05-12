using FluentValidation;

namespace Aura.Application.RoommateGroups.Commands.CreateRoommateGroup;

public class CreateRoommateGroupCommandValidator : AbstractValidator<CreateRoommateGroupCommand>
{
    public CreateRoommateGroupCommandValidator()
    {
        RuleFor(x => x.RoomSizePreference).IsInEnum();
    }
}
