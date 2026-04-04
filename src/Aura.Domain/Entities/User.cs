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
    public bool IsEmailConfirmed { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? MatriculationCode { get; private set; }
    public Guid? FacultyId { get; private set; }
    public Faculty? Faculty { get; private set; }

    private User() { }

    public static User Create(string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email is required.");
        if (email.Length > 256)
            throw new DomainException("Email must not exceed 256 characters.");
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash is required.");

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            FirstName = string.Empty,
            LastName = string.Empty,
            PasswordHash = passwordHash,
            Role = UserRole.Student,
            IsEmailConfirmed = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void ConfirmEmail()
    {
        if (IsEmailConfirmed)
            throw new DomainException("Email is already confirmed.");

        IsEmailConfirmed = true;
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

    public void AssignToFaculty(Guid facultyId)
    {
        if (Role != UserRole.FacultyAdmin && Role != UserRole.Student)
            throw new DomainException("Only FacultyAdmin or Student users can be assigned to a faculty.");
        if (facultyId == Guid.Empty)
            throw new DomainException("Faculty ID is required.");

        FacultyId = facultyId;
    }

    public void RemoveFromFaculty()
    {
        FacultyId = null;
    }

    public void SetMatriculationCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Matriculation code is required.");
        if (code.Length > 50)
            throw new DomainException("Matriculation code must not exceed 50 characters.");

        MatriculationCode = code.Trim();
    }
}
