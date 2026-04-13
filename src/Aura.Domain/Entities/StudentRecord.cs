using Aura.Domain.Enums;
using Aura.Domain.Exceptions;

namespace Aura.Domain.Entities;

public class StudentRecord
{
    public Guid Id { get; private set; }
    public string MatriculationCode { get; private set; } = null!;
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public int Points { get; private set; }
    public Gender Gender { get; private set; }
    public Guid FacultyId { get; private set; }
    public Faculty? Faculty { get; private set; }
    public Guid AllocationPeriodId { get; private set; }
    public AllocationPeriod? AllocationPeriod { get; private set; }
    public Guid? UserId { get; private set; }
    public User? User { get; private set; }

    private StudentRecord() { }

    public static StudentRecord Create(
        string matriculationCode,
        string firstName,
        string lastName,
        int points,
        Gender gender,
        Guid facultyId,
        Guid allocationPeriodId)
    {
        if (string.IsNullOrWhiteSpace(matriculationCode))
            throw new DomainException("Matriculation code is required.");
        if (matriculationCode.Length > 50)
            throw new DomainException("Matriculation code must not exceed 50 characters.");
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required.");
        if (firstName.Length > 100)
            throw new DomainException("First name must not exceed 100 characters.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required.");
        if (lastName.Length > 100)
            throw new DomainException("Last name must not exceed 100 characters.");
        if (points < 0)
            throw new DomainException("Points must be greater than or equal to zero.");
        if (!Enum.IsDefined(gender))
            throw new DomainException("Invalid gender value.");
        if (facultyId == Guid.Empty)
            throw new DomainException("Faculty ID is required.");
        if (allocationPeriodId == Guid.Empty)
            throw new DomainException("Allocation period ID is required.");

        return new StudentRecord
        {
            Id = Guid.NewGuid(),
            MatriculationCode = matriculationCode.Trim(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Points = points,
            Gender = gender,
            FacultyId = facultyId,
            AllocationPeriodId = allocationPeriodId
        };
    }

    public void LinkToUser(Guid userId)
    {
        if (UserId is not null)
            throw new DomainException("Student record is already linked to a user.");

        UserId = userId;
    }
}
