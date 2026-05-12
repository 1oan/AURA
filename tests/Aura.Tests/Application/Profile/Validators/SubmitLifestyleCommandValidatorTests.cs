using Aura.Application.Profile.Commands.SubmitLifestyle;
using Aura.Domain.Enums;
using FluentValidation.TestHelper;

namespace Aura.Tests.Application.Profile.Validators;

public class SubmitLifestyleCommandValidatorTests
{
    private readonly SubmitLifestyleCommandValidator _validator = new();

    private static SubmitLifestyleCommand Valid(int cleanliness = 3) => new(
        SleepSchedule.Normal, WakeUpTime.Normal, cleanliness,
        NoiseTolerance.Some, StudyLocation.Mixed,
        GuestFrequency.Weekly, SmokingHabit.No);

    [Fact]
    public void ValidCommand_NoErrors()
    {
        var result = _validator.TestValidate(Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Cleanliness_OutOfRange_HasError(int value)
    {
        var result = _validator.TestValidate(Valid(value));
        result.ShouldHaveValidationErrorFor(x => x.CleanlinessLevel);
    }

    [Fact]
    public void InvalidEnum_HasError()
    {
        var cmd = new SubmitLifestyleCommand(
            (SleepSchedule)99, WakeUpTime.Normal, 3,
            NoiseTolerance.Some, StudyLocation.Mixed,
            GuestFrequency.Weekly, SmokingHabit.No);
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.SleepSchedule);
    }
}
