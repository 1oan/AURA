using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using FluentAssertions;

namespace Aura.Tests.Domain.Entities;

public class UserTests
{
    private const string ValidEmail = "ioan.virlescu@student.uaic.ro";
    private const string ValidHash = "hashed-password";

    // ─── Create() ────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_ReturnsUserWithStudentRoleAndUnconfirmedEmail()
    {
        var user = User.Create(ValidEmail, ValidHash);

        user.Id.Should().NotBe(Guid.Empty);
        user.Email.Should().Be(ValidEmail);
        user.PasswordHash.Should().Be(ValidHash);
        user.Role.Should().Be(UserRole.Student);
        user.IsEmailConfirmed.Should().BeFalse();
        user.FirstName.Should().BeEmpty();
        user.LastName.Should().BeEmpty();
        user.Gender.Should().BeNull();
        user.FacultyId.Should().BeNull();
        user.MatriculationCode.Should().BeNull();
    }

    [Fact]
    public void Create_NormalizesEmailToLowercaseAndTrimmed()
    {
        var user = User.Create("  Ioan.Virlescu@Student.UAIC.RO  ", ValidHash);

        user.Email.Should().Be("ioan.virlescu@student.uaic.ro");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyEmail_ThrowsDomainException(string? email)
    {
        var act = () => User.Create(email!, ValidHash);

        act.Should().Throw<DomainException>().WithMessage("Email is required.");
    }

    [Fact]
    public void Create_WithEmailExceeding256Chars_ThrowsDomainException()
    {
        var longEmail = new string('a', 250) + "@uaic.ro";

        var act = () => User.Create(longEmail, ValidHash);

        act.Should().Throw<DomainException>().WithMessage("Email must not exceed 256 characters.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyPasswordHash_ThrowsDomainException(string? hash)
    {
        var act = () => User.Create(ValidEmail, hash!);

        act.Should().Throw<DomainException>().WithMessage("Password hash is required.");
    }

    // ─── ConfirmEmail() ──────────────────────────────────────────────────

    [Fact]
    public void ConfirmEmail_WhenUnconfirmed_SetsIsEmailConfirmedToTrue()
    {
        var user = User.Create(ValidEmail, ValidHash);

        user.ConfirmEmail();

        user.IsEmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public void ConfirmEmail_WhenAlreadyConfirmed_ThrowsDomainException()
    {
        var user = User.Create(ValidEmail, ValidHash);
        user.ConfirmEmail();

        var act = () => user.ConfirmEmail();

        act.Should().Throw<DomainException>().WithMessage("Email is already confirmed.");
    }

    // ─── UpdateProfile() ─────────────────────────────────────────────────

    [Fact]
    public void UpdateProfile_WithValidNames_UpdatesFirstNameAndLastName()
    {
        var user = User.Create(ValidEmail, ValidHash);

        user.UpdateProfile("Ioan", "Vîrlescu");

        user.FirstName.Should().Be("Ioan");
        user.LastName.Should().Be("Vîrlescu");
    }

    [Fact]
    public void UpdateProfile_TrimsWhitespace()
    {
        var user = User.Create(ValidEmail, ValidHash);

        user.UpdateProfile("  Ioan  ", "  Vîrlescu  ");

        user.FirstName.Should().Be("Ioan");
        user.LastName.Should().Be("Vîrlescu");
    }

    [Theory]
    [InlineData("", "Vîrlescu")]
    [InlineData("Ioan", "")]
    [InlineData("   ", "Vîrlescu")]
    public void UpdateProfile_WithEmptyNames_ThrowsDomainException(string firstName, string lastName)
    {
        var user = User.Create(ValidEmail, ValidHash);

        var act = () => user.UpdateProfile(firstName, lastName);

        act.Should().Throw<DomainException>();
    }

    // ─── SetRole() ───────────────────────────────────────────────────────

    [Fact]
    public void SetRole_WithDifferentRole_UpdatesRole()
    {
        var user = User.Create(ValidEmail, ValidHash);

        user.SetRole(UserRole.FacultyAdmin);

        user.Role.Should().Be(UserRole.FacultyAdmin);
    }

    [Fact]
    public void SetRole_WithSameRole_ThrowsDomainException()
    {
        var user = User.Create(ValidEmail, ValidHash);

        var act = () => user.SetRole(UserRole.Student);

        act.Should().Throw<DomainException>().WithMessage("User already has the Student role.");
    }

    // ─── AssignToFaculty() ───────────────────────────────────────────────

    [Fact]
    public void AssignToFaculty_WhenStudent_SetsFacultyId()
    {
        var user = User.Create(ValidEmail, ValidHash);
        var facultyId = Guid.NewGuid();

        user.AssignToFaculty(facultyId);

        user.FacultyId.Should().Be(facultyId);
    }

    [Fact]
    public void AssignToFaculty_WhenFacultyAdmin_SetsFacultyId()
    {
        var user = User.Create(ValidEmail, ValidHash);
        user.SetRole(UserRole.FacultyAdmin);
        var facultyId = Guid.NewGuid();

        user.AssignToFaculty(facultyId);

        user.FacultyId.Should().Be(facultyId);
    }

    [Fact]
    public void AssignToFaculty_WhenSuperAdmin_ThrowsDomainException()
    {
        var user = User.Create(ValidEmail, ValidHash);
        user.SetRole(UserRole.SuperAdmin);

        var act = () => user.AssignToFaculty(Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AssignToFaculty_WithEmptyGuid_ThrowsDomainException()
    {
        var user = User.Create(ValidEmail, ValidHash);

        var act = () => user.AssignToFaculty(Guid.Empty);

        act.Should().Throw<DomainException>().WithMessage("Faculty ID is required.");
    }

    // ─── SetMatriculationCode() ──────────────────────────────────────────

    [Fact]
    public void SetMatriculationCode_WithValidCode_SetsCode()
    {
        var user = User.Create(ValidEmail, ValidHash);

        user.SetMatriculationCode("CS2024001");

        user.MatriculationCode.Should().Be("CS2024001");
    }

    [Fact]
    public void SetMatriculationCode_OverwritesExistingCode()
    {
        var user = User.Create(ValidEmail, ValidHash);
        user.SetMatriculationCode("OLD123");

        user.SetMatriculationCode("NEW456");

        user.MatriculationCode.Should().Be("NEW456");
    }

    // ─── SetGender() ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(Gender.Male)]
    [InlineData(Gender.Female)]
    public void SetGender_WithValidEnum_SetsGender(Gender gender)
    {
        var user = User.Create(ValidEmail, ValidHash);

        user.SetGender(gender);

        user.Gender.Should().Be(gender);
    }

    [Fact]
    public void SetGender_WithUndefinedEnumValue_ThrowsDomainException()
    {
        var user = User.Create(ValidEmail, ValidHash);

        var act = () => user.SetGender((Gender)999);

        act.Should().Throw<DomainException>().WithMessage("Invalid gender value.");
    }

    // ─── RecordLogin() ───────────────────────────────────────────────────
    [Fact]
    public void RecordLogin_WhenCalled_SetsLastLoginAtToCurrentUtcTime()
    {
        var user = User.Create(ValidEmail, ValidHash);

        user.RecordLogin();

        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
