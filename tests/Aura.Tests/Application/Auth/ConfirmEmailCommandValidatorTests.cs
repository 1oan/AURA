using Aura.Application.Auth.Commands.ConfirmEmail;
using FluentValidation.TestHelper;

namespace Aura.Tests.Application.Auth;

public class ConfirmEmailCommandValidatorTests
{
    private readonly ConfirmEmailCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValid6DigitCode_DoesNotHaveError()
    {
        var command = new ConfirmEmailCommand("123456");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyCode_HasError(string code)
    {
        var command = new ConfirmEmailCommand(code);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Confirmation code is required.");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("1234567")]
    public void Validate_WithWrongLength_HasError(string code)
    {
        var command = new ConfirmEmailCommand(code);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Confirmation code must be exactly 6 digits.");
    }

    [Theory]
    [InlineData("abcdef")]
    [InlineData("12345a")]
    [InlineData("12-456")]
    public void Validate_WithNonDigitChars_HasError(string code)
    {
        var command = new ConfirmEmailCommand(code);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Confirmation code must contain only digits.");
    }
}
