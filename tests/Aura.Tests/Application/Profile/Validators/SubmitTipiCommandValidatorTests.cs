using Aura.Application.Profile.Commands.SubmitTipi;
using FluentValidation.TestHelper;

namespace Aura.Tests.Application.Profile.Validators;

public class SubmitTipiCommandValidatorTests
{
    private readonly SubmitTipiCommandValidator _validator = new();

    [Fact]
    public void ValidAnswers_NoErrors()
    {
        var result = _validator.TestValidate(new SubmitTipiCommand(new[] { 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 }));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NullAnswers_HasError()
    {
        var result = _validator.TestValidate(new SubmitTipiCommand(null!));
        result.ShouldHaveValidationErrorFor(x => x.Answers);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(11)]
    public void WrongLength_HasError(int length)
    {
        var answers = Enumerable.Repeat(4, length).ToArray();
        var result = _validator.TestValidate(new SubmitTipiCommand(answers));
        result.ShouldHaveValidationErrorFor(x => x.Answers);
    }

    [Fact]
    public void OutOfRange_HasError()
    {
        var answers = new[] { 4, 4, 4, 4, 4, 4, 4, 4, 4, 8 };
        var result = _validator.TestValidate(new SubmitTipiCommand(answers));
        result.ShouldHaveValidationErrorFor(x => x.Answers);
    }
}
