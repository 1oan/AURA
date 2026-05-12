using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.RoommateGroups.Commands.CreateRoommateGroup;

public record CreateRoommateGroupCommand(RoomSizePreference RoomSizePreference) : IRequest<Guid>;

public class CreateRoommateGroupCommandHandler(
    ICurrentUserService currentUser,
    IRoommateGroupRepository groupRepository,
    IDormAllocationRepository allocationRepository,
    IAllocationPeriodRepository periodRepository) : IRequestHandler<CreateRoommateGroupCommand, Guid>
{
    public async Task<Guid> Handle(CreateRoommateGroupCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.GetCurrentUserId();

        var periods = await periodRepository.GetActiveAsync(cancellationToken);
        var activePeriod = periods.FirstOrDefault()
            ?? throw new DomainException("No active allocation period.");

        var allocation = await allocationRepository.FindActiveByUserAndPeriodAsync(userId, activePeriod.Id, cancellationToken)
            ?? throw new DomainException("You need an accepted dorm allocation to create a group.");
        if (allocation.Status != AllocationStatus.Accepted)
            throw new DomainException("You need an accepted dorm allocation to create a group.");

        var existing = await groupRepository.FindActiveByUserAndPeriodAsync(userId, activePeriod.Id, cancellationToken);
        if (existing is not null)
            throw new DomainException("You are already in an active group for this period.");

        var group = RoommateGroup.Create(activePeriod.Id, allocation.DormitoryId, userId, request.RoomSizePreference);
        await groupRepository.AddAsync(group, cancellationToken);
        await groupRepository.SaveChangesAsync(cancellationToken);

        return group.Id;
    }
}
