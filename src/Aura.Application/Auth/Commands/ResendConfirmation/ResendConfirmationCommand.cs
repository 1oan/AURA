using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.Auth.Commands.ResendConfirmation;

public record ResendConfirmationCommand : IRequest;

public class ResendConfirmationCommandHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IEmailConfirmationCodeRepository confirmationCodeRepository,
    IEmailService emailService) : IRequestHandler<ResendConfirmationCommand>
{
    public async Task Handle(ResendConfirmationCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.IsEmailConfirmed)
            throw new DomainException("Email is already confirmed.");

        // 60-second cooldown
        var latestCode = await confirmationCodeRepository.GetLatestCodeAsync(userId, cancellationToken);
        if (latestCode is not null && (DateTime.UtcNow - latestCode.CreatedAt).TotalSeconds < 60)
            throw new DomainException("Please wait before requesting a new code.");

        await confirmationCodeRepository.InvalidateExistingCodesAsync(userId, cancellationToken);

        var code = Random.Shared.Next(100000, 999999).ToString();
        var confirmationCode = EmailConfirmationCode.Create(userId, code);
        await confirmationCodeRepository.AddAsync(confirmationCode, cancellationToken);
        await confirmationCodeRepository.SaveChangesAsync(cancellationToken);

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
}