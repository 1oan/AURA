using Aura.Application.Common.Events;
using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.UpgradeRequests.Commands.SubmitUpgradeRequest;

public record SubmitUpgradeRequestCommand(Guid AllocationPeriodId, List<Guid> DormitoryIds) : IRequest<Guid>;

public class SubmitUpgradeRequestCommandHandler(
    ICurrentUserService currentUserService,
    IUpgradeRequestRepository upgradeRequestRepository,
    IDormAllocationRepository dormAllocationRepository,
    IFacultyRoomAllocationRepository facultyRoomAllocationRepository,
    IUserRepository userRepository,
    IAllocationPeriodRepository allocationPeriodRepository,
    IPublisher publisher) : IRequestHandler<SubmitUpgradeRequestCommand, Guid>
{
    public async Task<Guid> Handle(SubmitUpgradeRequestCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.FacultyId is null || user.Gender is null)
            throw new DomainException("You must participate in the allocation period before requesting an upgrade.");

        var period = await allocationPeriodRepository.FindByIdAsync(request.AllocationPeriodId, cancellationToken)
            ?? throw new NotFoundException("Allocation period not found.");

        if (period.Status != AllocationPeriodStatus.Open && period.Status != AllocationPeriodStatus.Allocating)
            throw new DomainException("Upgrade requests can only be submitted while the allocation period is open or allocating.");

        var current = await dormAllocationRepository.FindActiveByUserAndPeriodAsync(userId, request.AllocationPeriodId, cancellationToken)
            ?? throw new DomainException("You must have an active allocation to request an upgrade.");

        if (current.Status != AllocationStatus.Accepted)
            throw new DomainException("Only accepted allocations are eligible for upgrade requests.");

        await ValidateTargetsStructurally(
            request.DormitoryIds, user, current.DormitoryId, request.AllocationPeriodId, cancellationToken);

        var existing = await upgradeRequestRepository.FindActiveByUserAndPeriodAsync(userId, request.AllocationPeriodId, cancellationToken);
        if (existing is not null)
            existing.Cancel();

        var newRequest = UpgradeRequest.Create(userId, request.AllocationPeriodId, request.DormitoryIds);
        await upgradeRequestRepository.AddAsync(newRequest, cancellationToken);
        await upgradeRequestRepository.SaveChangesAsync(cancellationToken);

        if (existing is not null)
            await publisher.Publish(
                new UpgradeRequestCancelledEvent(existing.Id, userId, request.AllocationPeriodId),
                cancellationToken);

        await publisher.Publish(
            new UpgradeRequestSubmittedEvent(
                newRequest.Id, userId, request.AllocationPeriodId, request.DormitoryIds.AsReadOnly()),
            cancellationToken);

        return newRequest.Id;
    }

    private async Task ValidateTargetsStructurally(
        List<Guid> targetDormIds,
        User user,
        Guid currentDormId,
        Guid allocationPeriodId,
        CancellationToken cancellationToken)
    {
        if (targetDormIds.Contains(currentDormId))
            throw new DomainException("Upgrade targets must differ from your current dormitory.");

        var allocations = await facultyRoomAllocationRepository
            .GetByPeriodAndFacultyAsync(allocationPeriodId, user.FacultyId!.Value, cancellationToken);

        var roomsByDorm = allocations
            .GroupBy(a => a.Room.DormitoryId)
            .ToDictionary(g => g.Key, g => g.Select(a => a.Room).ToList());

        foreach (var dormId in targetDormIds)
        {
            if (!roomsByDorm.TryGetValue(dormId, out var rooms) || rooms.Count == 0)
                throw new DomainException(
                    $"Dormitory {dormId} is not allocated to your faculty for this period.");

            if (!rooms.Any(r => r.Gender == user.Gender!.Value))
                throw new DomainException(
                    $"Dormitory {dormId} has no rooms matching your gender for this period.");
        }
    }
}
