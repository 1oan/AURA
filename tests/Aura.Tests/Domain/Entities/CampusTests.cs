using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class CampusTests
{
    // ─── Create() ────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithNameAndAddress_ReturnsPopulatedCampus()
    {
        var campus = Campus.Create("Codrescu", "Strada Codrescu, Iași");

        campus.Id.Should().NotBe(Guid.Empty);
        campus.Name.Should().Be("Codrescu");
        campus.Address.Should().Be("Strada Codrescu, Iași");
        campus.Dormitories.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithoutAddress_SetsAddressToNull()
    {
        var campus = Campus.Create("Codrescu");

        campus.Address.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsFields()
    {
        var campus = Campus.Create("  Codrescu  ", "  Strada Codrescu  ");

        campus.Name.Should().Be("Codrescu");
        campus.Address.Should().Be("Strada Codrescu");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ThrowsDomainException(string? name)
    {
        var act = () => Campus.Create(name!);

        act.Should().Throw<DomainException>().WithMessage("Campus name is required.");
    }

    [Fact]
    public void Create_WithNameExceeding200Chars_ThrowsDomainException()
    {
        var longName = new string('a', 201);

        var act = () => Campus.Create(longName);

        act.Should().Throw<DomainException>().WithMessage("Campus name must not exceed 200 characters.");
    }

    [Fact]
    public void Create_WithAddressExceeding500Chars_ThrowsDomainException()
    {
        var longAddress = new string('a', 501);

        var act = () => Campus.Create("Codrescu", longAddress);

        act.Should().Throw<DomainException>().WithMessage("Campus address must not exceed 500 characters.");
    }

    // ─── Update() ────────────────────────────────────────────────────────

    [Fact]
    public void Update_WithValidInputs_UpdatesFields()
    {
        var campus = Campus.Create("Old Name");

        campus.Update("New Name", "New Address");

        campus.Name.Should().Be("New Name");
        campus.Address.Should().Be("New Address");
    }

    [Fact]
    public void Update_WithNullAddress_ClearsAddress()
    {
        var campus = Campus.Create("Name", "Address");

        campus.Update("Name");

        campus.Address.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_WithEmptyName_ThrowsDomainException(string name)
    {
        var campus = Campus.Create("Codrescu");

        var act = () => campus.Update(name);

        act.Should().Throw<DomainException>().WithMessage("Campus name is required.");
    }

    [Fact]
    public void Update_WithNameExceeding200Chars_ThrowsDomainException()
    {
        var campus = Campus.Create("Codrescu");
        var longName = new string('a', 201);

        var act = () => campus.Update(longName);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_WithAddressExceeding500Chars_ThrowsDomainException()
    {
        var campus = Campus.Create("Codrescu");
        var longAddress = new string('a', 501);

        var act = () => campus.Update("Codrescu", longAddress);

        act.Should().Throw<DomainException>();
    }
}
