using MediatR;

namespace Aura.Application.Common.Events;

public record AllocationReminderDueEvent(
    Guid AllocationId,
    Guid UserId,
    Guid DormitoryId,
    Guid AllocationPeriodId) : INotification;
