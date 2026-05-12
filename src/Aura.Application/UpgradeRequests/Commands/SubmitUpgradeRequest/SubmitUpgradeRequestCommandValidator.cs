using FluentValidation;

namespace Aura.Application.UpgradeRequests.Commands.SubmitUpgradeRequest;

public class SubmitUpgradeRequestCommandValidator : AbstractValidator<SubmitUpgradeRequestCommand>
{
    public SubmitUpgradeRequestCommandValidator()
    {
        RuleFor(x => x.AllocationPeriodId)
            .NotEmpty().WithMessage("Allocation period ID is required.");

        RuleFor(x => x.DormitoryIds)
            .NotEmpty().WithMessage("At least one upgrade target is required.");

        RuleFor(x => x.DormitoryIds)
            .Must(ids => ids.All(id => id != Guid.Empty))
            .WithMessage("Upgrade target IDs must not be empty.")
            .When(x => x.DormitoryIds is not null && x.DormitoryIds.Count > 0);

        RuleFor(x => x.DormitoryIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Upgrade targets must not contain duplicates.")
            .When(x => x.DormitoryIds is not null && x.DormitoryIds.Count > 0);
    }
}
