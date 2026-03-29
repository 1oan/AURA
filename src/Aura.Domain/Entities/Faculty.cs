using Aura.Domain.Exceptions;

namespace Aura.Domain.Entities;

public class Faculty
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Abbreviation { get; private set; } = null!;

    private Faculty() { }

    public static Faculty Create(string name, string abbreviation)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Faculty name is required.");
        if (name.Length > 200)
            throw new DomainException("Faculty name must not exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(abbreviation))
            throw new DomainException("Faculty abbreviation is required.");
        if (abbreviation.Length > 20)
            throw new DomainException("Faculty abbreviation must not exceed 20 characters.");

        return new Faculty
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Abbreviation = abbreviation.Trim().ToUpperInvariant()
        };
    }

    public void Update(string name, string abbreviation)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Faculty name is required.");
        if (name.Length > 200)
            throw new DomainException("Faculty name must not exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(abbreviation))
            throw new DomainException("Faculty abbreviation is required.");
        if (abbreviation.Length > 20)
            throw new DomainException("Faculty abbreviation must not exceed 20 characters.");

        Name = name.Trim();
        Abbreviation = abbreviation.Trim().ToUpperInvariant();
    }
}
