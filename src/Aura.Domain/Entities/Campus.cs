namespace Aura.Domain.Entities;

using Aura.Domain.Exceptions;

public class Campus
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Address { get; private set; }

    private readonly List<Dormitory> _dormitories = [];
    public IReadOnlyCollection<Dormitory> Dormitories => _dormitories.AsReadOnly();

    private Campus() { }

    public static Campus Create(string name, string? address = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Campus name is required.");
        if (name.Length > 200)
            throw new DomainException("Campus name must not exceed 200 characters.");
        if (address is not null && address.Length > 500)
            throw new DomainException("Campus address must not exceed 500 characters.");

        return new Campus
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Address = address?.Trim()
        };
    }

    public void Update(string name, string? address = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Campus name is required.");
        if (name.Length > 200)
            throw new DomainException("Campus name must not exceed 200 characters.");
        if (address is not null && address.Length > 500)
            throw new DomainException("Campus address must not exceed 500 characters.");

        Name = name.Trim();
        Address = address?.Trim();
    }
}
