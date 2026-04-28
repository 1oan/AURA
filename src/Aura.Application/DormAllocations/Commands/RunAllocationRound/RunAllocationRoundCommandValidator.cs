using FluentValidation;

namespace Aura.Application.DormAllocations.Commands.RunAllocationRound;

public class RunAllocationRoundCommandValidator : AbstractValidator<RunAllocationRoundCommand>
{
    public RunAllocationRoundCommandValidator()
    {
        RuleFor(c => c.AllocationPeriodId).NotEmpty();
        RuleFor(c => c.Round).GreaterThanOrEqualTo(1);
    }
}
