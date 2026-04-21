using Aura.Application.Auth.Commands.Register;
using FluentValidation.TestHelper;

namespace Aura.Tests.Application.Auth;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    // ─── Email validation ────────────────────────────────────────────────

    [Theory]
    [InlineData("student@uaic.ro")]
    [InlineData("ioan.virlescu@student.uaic.ro")]
    [InlineData("admin@info.uaic.ro")]
    [InlineData("USER@UAIC.RO")]
    public void Validate_WithValidInstitutionalEmail_DoesNotHaveError(string email)
    {
        var command = new RegisterUserCommand(email, "password123");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyEmail_HasError(string email)
    {
        var command = new RegisterUserCommand(email, "password123");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Theory]
    [InlineData("user@gmail.com")]
    [InlineData("student@yahoo.com")]
    [InlineData("admin@example.com")]
    public void Validate_WithNonInstitutionalEmail_HasError(string email)
    {
        var command = new RegisterUserCommand(email, "password123");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Only institutional emails ending in uaic.ro are allowed.");
    }

    [Fact]
    public void Validate_WithMalformedEmail_HasError()
    {
        var command = new RegisterUserCommand("not-an-email", "password123");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithEmailExceeding256Chars_HasError()
    {
        var longEmail = new string('a', 250) + "@uaic.ro";
        var command = new RegisterUserCommand(longEmail, "password123");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    // ─── Password validation ─────────────────────────────────────────────

    [Fact]
    public void Validate_WithValidPassword_DoesNotHaveError()
    {
        var command = new RegisterUserCommand("user@uaic.ro", "password123");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyPassword_HasError(string password)
    {
        var command = new RegisterUserCommand("user@uaic.ro", password);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    public void Validate_WithPasswordTooShort_HasError(string password)
    {
        var command = new RegisterUserCommand("user@uaic.ro", password);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters.");
    }

    [Fact]
    public void Validate_WithPasswordAt8Chars_DoesNotHaveError()
    {
        var command = new RegisterUserCommand("user@uaic.ro", "12345678");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
