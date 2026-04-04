using Aura.Domain.Exceptions;

namespace Aura.Domain.Entities;

public class EmailConfirmationCode
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Code { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public bool IsUsed { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private EmailConfirmationCode() { }

    public static EmailConfirmationCode Create(Guid userId, string code, int expiryMinutes = 15)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User ID is required.");
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            throw new DomainException("Code must be exactly 6 characters.");

        return new EmailConfirmationCode
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkAsUsed()
    {
        if (IsUsed)
            throw new DomainException("Confirmation code has already been used.");
        if (DateTime.UtcNow > ExpiresAt)
            throw new DomainException("Confirmation code has expired.");

        IsUsed = true;
    }
}