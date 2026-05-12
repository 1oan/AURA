using FluentValidation;

namespace Aura.Application.Profile.Commands.SubmitLifestyle;

public class SubmitLifestyleCommandValidator : AbstractValidator<SubmitLifestyleCommand>
{
    public SubmitLifestyleCommandValidator()
    {
        RuleFor(x => x.SleepSchedule).IsInEnum();
        RuleFor(x => x.WakeUpTime).IsInEnum();
        RuleFor(x => x.NoiseTolerance).IsInEnum();
        RuleFor(x => x.StudyLocation).IsInEnum();
        RuleFor(x => x.GuestFrequency).IsInEnum();
        RuleFor(x => x.SmokingHabit).IsInEnum();
        RuleFor(x => x.CleanlinessLevel).InclusiveBetween(1, 5);
    }
}
