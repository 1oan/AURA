using FluentValidation;

namespace Aura.Application.RoommateGroups.Commands.InviteToGroup;

public class InviteToGroupCommandValidator : AbstractValidator<InviteToGroupCommand>
{
    public InviteToGroupCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.InviteeUserId).NotEmpty();
    }
}
