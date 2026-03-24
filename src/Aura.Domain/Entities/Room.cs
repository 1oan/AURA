namespace Aura.Domain.Entities;

using Aura.Domain.Enums;
using Aura.Domain.Exceptions;

public class Room
{
    public Guid Id { get; private set; }
    public string Number { get; private set; } = null!;
    public Guid DormitoryId { get; private set; }
    public int Floor { get; private set; }
    public int Capacity { get; private set; }
    public Gender Gender { get; private set; }
    public Dormitory Dormitory { get; private set; } = null!;

    private Room() { }

    public static Room Create(string number, Guid dormitoryId, int floor, int capacity, Gender gender)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new DomainException("Room number is required.");
        if (number.Length > 20)
            throw new DomainException("Room number must not exceed 20 characters.");
        if (dormitoryId == Guid.Empty)
            throw new DomainException("Dormitory ID is required.");
        if (floor < 0)
            throw new DomainException("Floor must be zero or positive.");
        if (capacity < 1 || capacity > 10)
            throw new DomainException("Room capacity must be between 1 and 10.");
        if (!Enum.IsDefined(gender))
            throw new DomainException("Invalid gender value.");

        return new Room
        {
            Id = Guid.NewGuid(),
            Number = number.Trim(),
            DormitoryId = dormitoryId,
            Floor = floor,
            Capacity = capacity,
            Gender = gender
        };
    }

    public void Update(string number, int floor, int capacity, Gender gender)
    {
        if (string.IsNullOrWhiteSpace(number))
            throw new DomainException("Room number is required.");
        if (number.Length > 20)
            throw new DomainException("Room number must not exceed 20 characters.");
        if (floor < 0)
            throw new DomainException("Floor must be zero or positive.");
        if (capacity < 1 || capacity > 10)
            throw new DomainException("Room capacity must be between 1 and 10.");
        if (!Enum.IsDefined(gender))
            throw new DomainException("Invalid gender value.");

        Number = number.Trim();
        Floor = floor;
        Capacity = capacity;
        Gender = gender;
    }
}
