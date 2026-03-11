using Aura.Application.Auth.Common;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Auth.Commands.Register;

public record RegisterUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password) : IRequest<AuthResult>;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, AuthResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken))
            throw new DomainException("A user with this email already exists.");

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = User.Create(request.Email, request.FirstName, request.LastName, passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        var token = _jwtTokenGenerator.GenerateToken(user);

        return new AuthResult(
            token,
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role.ToString());
    }
}
