namespace Aura.Domain.Entities;

using Aura.Domain.Exceptions;

public class Dormitory
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public Guid CampusId { get; private set; }
    public Campus Campus { get; private set; } = null!;

    private readonly List<Room> _rooms = [];
    public IReadOnlyCollection<Room> Rooms => _rooms.AsReadOnly();

    private Dormitory() { }

    public static Dormitory Create(string name, Guid campusId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Dormitory name is required.");
        if (name.Length > 200)
            throw new DomainException("Dormitory name must not exceed 200 characters.");
        if (campusId == Guid.Empty)
            throw new DomainException("Campus ID is required.");

        return new Dormitory
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            CampusId = campusId
        };
    }

    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Dormitory name is required.");
        if (name.Length > 200)
            throw new DomainException("Dormitory name must not exceed 200 characters.");

        Name = name.Trim();
    }
}
