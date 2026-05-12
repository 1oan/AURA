using FluentValidation;

namespace Aura.Application.RoommateGroups.Commands.ChangeRoomSizePreference;

public class ChangeRoomSizePreferenceCommandValidator : AbstractValidator<ChangeRoomSizePreferenceCommand>
{
    public ChangeRoomSizePreferenceCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.NewPreference).IsInEnum();
    }
}
