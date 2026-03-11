using Aura.Domain.Enums;
using FluentValidation;

namespace Aura.Application.Users.Commands.PromoteUser;

public class PromoteUserCommandValidator : AbstractValidator<PromoteUserCommand>
{
    public PromoteUserCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();

        RuleFor(x => x.Role)
            .IsInEnum()
            .WithMessage("Invalid role value.");
    }
}
