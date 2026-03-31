using Aura.Application.Common.Interfaces;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Users.Commands.SetMatriculationCode;

public record SetMatriculationCodeCommand(string MatriculationCode) : IRequest<Unit>;

public class SetMatriculationCodeCommandHandler(
    IUserRepository userRepository,
    ICurrentUserService currentUserService) : IRequestHandler<SetMatriculationCodeCommand, Unit>
{
    public async Task<Unit> Handle(SetMatriculationCodeCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        user.SetMatriculationCode(request.MatriculationCode);
        await userRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
