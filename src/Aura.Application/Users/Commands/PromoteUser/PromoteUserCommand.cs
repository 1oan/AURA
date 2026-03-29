using Aura.Application.Common.Interfaces;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Users.Commands.PromoteUser;

public record PromoteUserCommand(Guid UserId, UserRole Role) : IRequest<Unit>;

public class PromoteUserCommandHandler : IRequestHandler<PromoteUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;

    public PromoteUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Unit> Handle(PromoteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.FindByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        user.SetRole(request.Role);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
