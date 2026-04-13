using Aura.Application.Common.Interfaces;
using Aura.Application.DormPreferences.Queries.GetAvailableDormitories;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Aura.Domain.Exceptions;
using MediatR;

namespace Aura.Application.DormPreferences.Commands.SubmitPreferences;

public record SubmitPreferencesCommand(Guid AllocationPeriodId, List<Guid> DormitoryIds) : IRequest;

public class SubmitPreferencesCommandHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IAllocationPeriodRepository allocationPeriodRepository,
    IFacultyRoomAllocationRepository facultyRoomAllocationRepository,
    IDormPreferenceRepository dormPreferenceRepository) : IRequestHandler<SubmitPreferencesCommand>
{
    public async Task Handle(SubmitPreferencesCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserService.GetCurrentUserId();
        var user = await userRepository.FindByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.FacultyId is null || user.Gender is null)
            throw new DomainException("You must participate in the allocation period before submitting preferences.");

        var period = await allocationPeriodRepository.FindByIdAsync(command.AllocationPeriodId, cancellationToken)
            ?? throw new NotFoundException("Allocation period not found.");

        if (period.Status != AllocationPeriodStatus.Open)
            throw new DomainException("Preferences can only be submitted when the allocation period is open.");

        // Get available dormitories for this student
        var allocations = await facultyRoomAllocationRepository
            .GetByPeriodAndFacultyAsync(command.AllocationPeriodId, user.FacultyId.Value, cancellationToken);

        var availableDormIds = allocations
            .Where(a => a.Room.Gender == user.Gender.Value)
            .Select(a => a.Room.DormitoryId)
            .Distinct()
            .ToHashSet();

        // Validate: all submitted dorms must be available
        var submittedSet = command.DormitoryIds.ToHashSet();
        if (!submittedSet.SetEquals(availableDormIds))
            throw new DomainException("You must rank all available dormitories — no more, no less.");

        // Replace existing preferences
        await dormPreferenceRepository.DeleteByUserAndPeriodAsync(userId, command.AllocationPeriodId, cancellationToken);

        var preferences = command.DormitoryIds.Select((dormId, index) =>
            DormPreference.Create(userId, command.AllocationPeriodId, dormId, index + 1));

        await dormPreferenceRepository.AddRangeAsync(preferences, cancellationToken);
        await dormPreferenceRepository.SaveChangesAsync(cancellationToken);
    }
}
