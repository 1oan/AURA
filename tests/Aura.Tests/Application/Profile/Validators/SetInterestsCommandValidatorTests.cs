using Aura.Application.Profile.Commands.SetInterests;
using FluentValidation.TestHelper;

namespace Aura.Tests.Application.Profile.Validators;

public class SetInterestsCommandValidatorTests
{
    private static readonly string[] FootballGaming = ["football", "gaming"];
    private static readonly string[] FootballDuplicate = ["football", "football"];
    private static readonly string[] FootballBlank = ["football", ""];

    private readonly SetInterestsCommandValidator _validator = new();

    [Fact]
    public void Valid_NoErrors()
    {
        var result = _validator.TestValidate(new SetInterestsCommand(FootballGaming));
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
        var result = _validator.TestValidate(new SetInterestsCommand(FootballDuplicate));
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
        var result = _validator.TestValidate(new SetInterestsCommand(FootballBlank));
        result.ShouldHaveValidationErrorFor(x => x.Slugs);
    }
}
