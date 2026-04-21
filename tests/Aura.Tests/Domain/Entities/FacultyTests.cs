using Aura.Domain.Entities;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class FacultyTests
{
    // ─── Create() ────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_ReturnsPopulatedFaculty()
    {
        var faculty = Faculty.Create("Informatica", "INF");

        faculty.Id.Should().NotBe(Guid.Empty);
        faculty.Name.Should().Be("Informatica");
        faculty.Abbreviation.Should().Be("INF");
    }

    [Fact]
    public void Create_UppercasesAbbreviation()
    {
        var faculty = Faculty.Create("Informatica", "inf");

        faculty.Abbreviation.Should().Be("INF");
    }

    [Fact]
    public void Create_TrimsFields()
    {
        var faculty = Faculty.Create("  Informatica  ", "  inf  ");

        faculty.Name.Should().Be("Informatica");
        faculty.Abbreviation.Should().Be("INF");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ThrowsDomainException(string? name)
    {
        var act = () => Faculty.Create(name!, "INF");

        act.Should().Throw<DomainException>().WithMessage("Faculty name is required.");
    }

    [Fact]
    public void Create_WithNameExceeding200Chars_ThrowsDomainException()
    {
        var longName = new string('a', 201);

        var act = () => Faculty.Create(longName, "INF");

        act.Should().Throw<DomainException>().WithMessage("Faculty name must not exceed 200 characters.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyAbbreviation_ThrowsDomainException(string? abbrev)
    {
        var act = () => Faculty.Create("Informatica", abbrev!);

        act.Should().Throw<DomainException>().WithMessage("Faculty abbreviation is required.");
    }

    [Fact]
    public void Create_WithAbbreviationExceeding20Chars_ThrowsDomainException()
    {
        var longAbbrev = new string('A', 21);

        var act = () => Faculty.Create("Informatica", longAbbrev);

        act.Should().Throw<DomainException>().WithMessage("Faculty abbreviation must not exceed 20 characters.");
    }

    // ─── Update() ────────────────────────────────────────────────────────

    [Fact]
    public void Update_WithValidInputs_UpdatesFields()
    {
        var faculty = Faculty.Create("Old", "OLD");

        faculty.Update("New", "new");

        faculty.Name.Should().Be("New");
        faculty.Abbreviation.Should().Be("NEW");
    }

    [Theory]
    [InlineData("", "INF")]
    [InlineData("Informatica", "")]
    public void Update_WithEmptyField_ThrowsDomainException(string name, string abbrev)
    {
        var faculty = Faculty.Create("Informatica", "INF");

        var act = () => faculty.Update(name, abbrev);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_WithNameExceeding200Chars_ThrowsDomainException()
    {
        var faculty = Faculty.Create("Informatica", "INF");
        var longName = new string('a', 201);

        var act = () => faculty.Update(longName, "INF");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_WithAbbreviationExceeding20Chars_ThrowsDomainException()
    {
        var faculty = Faculty.Create("Informatica", "INF");
        var longAbbrev = new string('A', 21);

        var act = () => faculty.Update("Informatica", longAbbrev);

        act.Should().Throw<DomainException>();
    }
}
