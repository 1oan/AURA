using Aura.Domain.Enums;
using Aura.Domain.Exceptions;

namespace Aura.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    private User() { }

    public static User Create(string email, string firstName, string lastName, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");
        if (email.Length > 256)
            throw new DomainException("Email must not exceed 256 characters.");
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required.");
        if (firstName.Length > 100)
            throw new DomainException("First name must not exceed 100 characters.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required.");
        if (lastName.Length > 100)
            throw new DomainException("Last name must not exceed 100 characters.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PasswordHash = passwordHash,
            Role = UserRole.User,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required.");
        if (firstName.Length > 100)
            throw new DomainException("First name must not exceed 100 characters.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required.");
        if (lastName.Length > 100)
            throw new DomainException("Last name must not exceed 100 characters.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
    }

    public void ChangePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException("Password hash is required.");

        PasswordHash = newPasswordHash;
    }

    public void SetRole(UserRole role)
    {
        if (role == Role)
            throw new DomainException($"User already has the {role} role.");

        Role = role;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
}
