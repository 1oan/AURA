using Aura.Application.Profile.Commands.SetInterests;
using FluentValidation.TestHelper;

namespace Aura.Tests.Application.Profile.Validators;

public class SetInterestsCommandValidatorTests
{
    private readonly SetInterestsCommandValidator _validator = new();

    [Fact]
    public void Valid_NoErrors()
    {
        var result = _validator.TestValidate(new SetInterestsCommand(new[] { "football", "gaming" }));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_NoErrors()
    {
        var result = _validator.TestValidate(new SetInterestsCommand(Array.Empty<string>()));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Duplicates_HasError()
    {
        var result = _validator.TestValidate(new SetInterestsCommand(new[] { "football", "football" }));
        result.ShouldHaveValidationErrorFor(x => x.Slugs);
    }

    [Fact]
    public void TooMany_HasError()
    {
        var slugs = Enumerable.Range(1, 51).Select(i => $"slug-{i}").ToArray();
        var result = _validator.TestValidate(new SetInterestsCommand(slugs));
        result.ShouldHaveValidationErrorFor(x => x.Slugs);
    }

    [Fact]
    public void BlankSlug_HasError()
    {
        var result = _validator.TestValidate(new SetInterestsCommand(new[] { "football", "" }));
        result.ShouldHaveValidationErrorFor(x => x.Slugs);
    }
}
