using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class StudentRecordTests
{
    private const string ValidCode = "CS2024001";
    private const string ValidFirst = "Ioan";
    private const string ValidLast = "Vîrlescu";
    private const int ValidPoints = 547;
    private const Gender ValidGender = Gender.Male;
    private static readonly Guid ValidFacultyId = Guid.NewGuid();
    private static readonly Guid ValidPeriodId = Guid.NewGuid();

    private static StudentRecord ValidRecord() =>
        StudentRecord.Create(ValidCode, ValidFirst, ValidLast, ValidPoints, ValidGender, ValidFacultyId, ValidPeriodId);

    // ─── Create() ────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_ReturnsRecordWithExpectedFields()
    {
        var record = ValidRecord();

        record.Id.Should().NotBe(Guid.Empty);
        record.MatriculationCode.Should().Be(ValidCode);
        record.FirstName.Should().Be(ValidFirst);
        record.LastName.Should().Be(ValidLast);
        record.Points.Should().Be(ValidPoints);
        record.Gender.Should().Be(ValidGender);
        record.FacultyId.Should().Be(ValidFacultyId);
        record.AllocationPeriodId.Should().Be(ValidPeriodId);
        record.UserId.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsStringFields()
    {
        var record = StudentRecord.Create(
            "  CS2024001  ", "  Ioan  ", "  Vîrlescu  ",
            ValidPoints, ValidGender, ValidFacultyId, ValidPeriodId);

        record.MatriculationCode.Should().Be("CS2024001");
        record.FirstName.Should().Be("Ioan");
        record.LastName.Should().Be("Vîrlescu");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyMatriculationCode_ThrowsDomainException(string? code)
    {
        var act = () => StudentRecord.Create(code!, ValidFirst, ValidLast, ValidPoints, ValidGender, ValidFacultyId, ValidPeriodId);

        act.Should().Throw<DomainException>().WithMessage("Matriculation code is required.");
    }

    [Fact]
    public void Create_WithMatriculationCodeExceeding50Chars_ThrowsDomainException()
    {
        var longCode = new string('A', 51);

        var act = () => StudentRecord.Create(longCode, ValidFirst, ValidLast, ValidPoints, ValidGender, ValidFacultyId, ValidPeriodId);

        act.Should().Throw<DomainException>().WithMessage("Matriculation code must not exceed 50 characters.");
    }

    [Fact]
    public void Create_WithNegativePoints_ThrowsDomainException()
    {
        var act = () => StudentRecord.Create(ValidCode, ValidFirst, ValidLast, -1, ValidGender, ValidFacultyId, ValidPeriodId);

        act.Should().Throw<DomainException>().WithMessage("Points must be greater than or equal to zero.");
    }

    [Fact]
    public void Create_WithZeroPoints_Succeeds()
    {
        var record = StudentRecord.Create(ValidCode, ValidFirst, ValidLast, 0, ValidGender, ValidFacultyId, ValidPeriodId);

        record.Points.Should().Be(0);
    }

    [Fact]
    public void Create_WithUndefinedGender_ThrowsDomainException()
    {
        var act = () => StudentRecord.Create(ValidCode, ValidFirst, ValidLast, ValidPoints, (Gender)999, ValidFacultyId, ValidPeriodId);

        act.Should().Throw<DomainException>().WithMessage("Invalid gender value.");
    }

    [Fact]
    public void Create_WithEmptyFacultyId_ThrowsDomainException()
    {
        var act = () => StudentRecord.Create(ValidCode, ValidFirst, ValidLast, ValidPoints, ValidGender, Guid.Empty, ValidPeriodId);

        act.Should().Throw<DomainException>().WithMessage("Faculty ID is required.");
    }

    [Fact]
    public void Create_WithEmptyPeriodId_ThrowsDomainException()
    {
        var act = () => StudentRecord.Create(ValidCode, ValidFirst, ValidLast, ValidPoints, ValidGender, ValidFacultyId, Guid.Empty);

        act.Should().Throw<DomainException>().WithMessage("Allocation period ID is required.");
    }

    // ─── LinkToUser() ────────────────────────────────────────────────────

    [Fact]
    public void LinkToUser_WhenUnlinked_SetsUserId()
    {
        var record = ValidRecord();
        var userId = Guid.NewGuid();

        record.LinkToUser(userId);

        record.UserId.Should().Be(userId);
    }

    [Fact]
    public void LinkToUser_WhenAlreadyLinked_ThrowsDomainException()
    {
        var record = ValidRecord();
        record.LinkToUser(Guid.NewGuid());

        var act = () => record.LinkToUser(Guid.NewGuid());

        act.Should().Throw<DomainException>().WithMessage("Student record is already linked to a user.");
    }
}
