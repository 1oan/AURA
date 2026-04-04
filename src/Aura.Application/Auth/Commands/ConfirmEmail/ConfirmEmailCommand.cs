using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Auth.Commands.ConfirmEmail;

public record ConfirmEmailCommand(string Code) : IRequest;

public class ConfirmEmailCommandHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IEmailConfirmationCodeRepository confirmationCodeRepository) : IRequestHandler<ConfirmEmailCommand>
{
    public async Task Handle(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.IsEmailConfirmed)
            throw new DomainException("Email is already confirmed.");

        var code = await confirmationCodeRepository.FindValidCodeAsync(userId, command.Code, cancellationToken)
            ?? throw new DomainException("Invalid or expired confirmation code.");

        code.MarkAsUsed();
        user.ConfirmEmail();

        await userRepository.SaveChangesAsync(cancellationToken);
    }
}