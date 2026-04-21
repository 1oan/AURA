using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class RoomTests
{
    private static readonly Guid ValidDormId = Guid.NewGuid();

    // ─── Create() ────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_ReturnsPopulatedRoom()
    {
        var room = Room.Create("101", ValidDormId, floor: 1, capacity: 3, Gender.Male);

        room.Id.Should().NotBe(Guid.Empty);
        room.Number.Should().Be("101");
        room.DormitoryId.Should().Be(ValidDormId);
        room.Floor.Should().Be(1);
        room.Capacity.Should().Be(3);
        room.Gender.Should().Be(Gender.Male);
    }

    [Fact]
    public void Create_TrimsNumber()
    {
        var room = Room.Create("  101  ", ValidDormId, 1, 3, Gender.Male);

        room.Number.Should().Be("101");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyNumber_ThrowsDomainException(string? number)
    {
        var act = () => Room.Create(number!, ValidDormId, 1, 3, Gender.Male);

        act.Should().Throw<DomainException>().WithMessage("Room number is required.");
    }

    [Fact]
    public void Create_WithNumberExceeding20Chars_ThrowsDomainException()
    {
        var longNumber = new string('1', 21);

        var act = () => Room.Create(longNumber, ValidDormId, 1, 3, Gender.Male);

        act.Should().Throw<DomainException>().WithMessage("Room number must not exceed 20 characters.");
    }

    [Fact]
    public void Create_WithEmptyDormitoryId_ThrowsDomainException()
    {
        var act = () => Room.Create("101", Guid.Empty, 1, 3, Gender.Male);

        act.Should().Throw<DomainException>().WithMessage("Dormitory ID is required.");
    }

    [Fact]
    public void Create_WithNegativeFloor_ThrowsDomainException()
    {
        var act = () => Room.Create("101", ValidDormId, -1, 3, Gender.Male);

        act.Should().Throw<DomainException>().WithMessage("Floor must be zero or positive.");
    }

    [Fact]
    public void Create_WithFloorZero_Succeeds()
    {
        var room = Room.Create("001", ValidDormId, 0, 3, Gender.Male);

        room.Floor.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Create_WithCapacityOutOfRange_ThrowsDomainException(int capacity)
    {
        var act = () => Room.Create("101", ValidDormId, 1, capacity, Gender.Male);

        act.Should().Throw<DomainException>().WithMessage("Room capacity must be between 1 and 10.");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public void Create_WithCapacityInRange_Succeeds(int capacity)
    {
        var room = Room.Create("101", ValidDormId, 1, capacity, Gender.Male);

        room.Capacity.Should().Be(capacity);
    }

    [Fact]
    public void Create_WithUndefinedGender_ThrowsDomainException()
    {
        var act = () => Room.Create("101", ValidDormId, 1, 3, (Gender)999);

        act.Should().Throw<DomainException>().WithMessage("Invalid gender value.");
    }

    // ─── Update() ────────────────────────────────────────────────────────

    [Fact]
    public void Update_WithValidInputs_UpdatesFields()
    {
        var room = Room.Create("101", ValidDormId, 1, 3, Gender.Male);

        room.Update("202", 2, 4, Gender.Female);

        room.Number.Should().Be("202");
        room.Floor.Should().Be(2);
        room.Capacity.Should().Be(4);
        room.Gender.Should().Be(Gender.Female);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithEmptyNumber_ThrowsDomainException(string number)
    {
        var room = Room.Create("101", ValidDormId, 1, 3, Gender.Male);

        var act = () => room.Update(number, 1, 3, Gender.Male);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_WithNumberExceeding20Chars_ThrowsDomainException()
    {
        var room = Room.Create("101", ValidDormId, 1, 3, Gender.Male);
        var longNumber = new string('1', 21);

        var act = () => room.Update(longNumber, 1, 3, Gender.Male);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_WithNegativeFloor_ThrowsDomainException()
    {
        var room = Room.Create("101", ValidDormId, 1, 3, Gender.Male);

        var act = () => room.Update("101", -1, 3, Gender.Male);

        act.Should().Throw<DomainException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Update_WithCapacityOutOfRange_ThrowsDomainException(int capacity)
    {
        var room = Room.Create("101", ValidDormId, 1, 3, Gender.Male);

        var act = () => room.Update("101", 1, capacity, Gender.Male);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_WithUndefinedGender_ThrowsDomainException()
    {
        var room = Room.Create("101", ValidDormId, 1, 3, Gender.Male);

        var act = () => room.Update("101", 1, 3, (Gender)999);

        act.Should().Throw<DomainException>();
    }
}
