using Aura.Application.Auth.Common;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Auth.Commands.Register;

public record RegisterUserCommand(
    string Email,
    string Password) : IRequest<AuthResult>;

public class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IEmailConfirmationCodeRepository confirmationCodeRepository,
    IEmailService emailService) : IRequestHandler<RegisterUserCommand, AuthResult>
{
    public async Task<AuthResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        if (await userRepository.ExistsByEmailAsync(request.Email.Trim().ToLowerInvariant(), cancellationToken))
            throw new DomainException("A user with this email already exists.");

        var passwordHash = passwordHasher.HashPassword(request.Password);
        var user = User.Create(request.Email, passwordHash);

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        // Generate and send confirmation code
        var code = Random.Shared.Next(100000, 999999).ToString();
        var confirmationCode = EmailConfirmationCode.Create(user.Id, code);
        await confirmationCodeRepository.AddAsync(confirmationCode, cancellationToken);
        await confirmationCodeRepository.SaveChangesAsync(cancellationToken);

        try
        {
            await emailService.SendEmailAsync(
                user.Email,
                "AURA — Confirm your email",
                $"""
                <h2>Welcome to AURA</h2>
                <p>Your confirmation code is:</p>
                <h1 style="letter-spacing: 8px; font-family: monospace;">{code}</h1>
                <p>This code expires in 15 minutes.</p>
                """,
                cancellationToken);
        }
        catch
        {
            // Email send failure should not block registration
        }

        var token = jwtTokenGenerator.GenerateToken(user);

        return new AuthResult(
            token,
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.IsEmailConfirmed);
    }
}