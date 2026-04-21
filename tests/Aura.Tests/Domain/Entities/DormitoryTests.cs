using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class DormitoryTests
{
    private static readonly Guid ValidCampusId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidInputs_ReturnsPopulatedDormitory()
    {
        var dormitory = Dormitory.Create("C1", ValidCampusId);

        dormitory.Id.Should().NotBe(Guid.Empty);
        dormitory.Name.Should().Be("C1");
        dormitory.CampusId.Should().Be(ValidCampusId);
        dormitory.Rooms.Should().BeEmpty();
    }

    [Fact]
    public void Create_TrimsName()
    {
        var dormitory = Dormitory.Create("  C1  ", ValidCampusId);

        dormitory.Name.Should().Be("C1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ThrowsDomainException(string? name)
    {
        var act = () => Dormitory.Create(name!, ValidCampusId);

        act.Should().Throw<DomainException>().WithMessage("Dormitory name is required.");
    }

    [Fact]
    public void Create_WithNameExceeding200Chars_ThrowsDomainException()
    {
        var longName = new string('a', 201);

        var act = () => Dormitory.Create(longName, ValidCampusId);

        act.Should().Throw<DomainException>().WithMessage("Dormitory name must not exceed 200 characters.");
    }

    [Fact]
    public void Create_WithEmptyCampusId_ThrowsDomainException()
    {
        var act = () => Dormitory.Create("C1", Guid.Empty);

        act.Should().Throw<DomainException>().WithMessage("Campus ID is required.");
    }

    [Fact]
    public void Update_WithValidName_UpdatesName()
    {
        var dormitory = Dormitory.Create("C1", ValidCampusId);

        dormitory.Update("C2");

        dormitory.Name.Should().Be("C2");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithEmptyName_ThrowsDomainException(string name)
    {
        var dormitory = Dormitory.Create("C1", ValidCampusId);

        var act = () => dormitory.Update(name);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_WithNameExceeding200Chars_ThrowsDomainException()
    {
        var dormitory = Dormitory.Create("C1", ValidCampusId);
        var longName = new string('a', 201);

        var act = () => dormitory.Update(longName);

        act.Should().Throw<DomainException>();
    }
}
