using Aura.Domain.Entities;
using Aura.Domain.Enums;

namespace Aura.Application.Common.Interfaces;

public interface IDormAllocationRepository
{
    Task<DormAllocation?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DormAllocation?> FindActiveByUserAndPeriodAsync(
        Guid userId, Guid allocationPeriodId, CancellationToken cancellationToken = default);

    Task<List<DormAllocation>> GetByPeriodAsync(
        Guid allocationPeriodId, CancellationToken cancellationToken = default);

    Task<List<DormAllocation>> GetByPeriodAndFacultyAsync(
        Guid allocationPeriodId, Guid facultyId, CancellationToken cancellationToken = default);

    Task<int> GetMaxRoundAsync(
        Guid allocationPeriodId, CancellationToken cancellationToken = default);

    Task<bool> HasTerminalForUserAndPeriodAsync(
        Guid userId, Guid allocationPeriodId, CancellationToken cancellationToken = default);

    Task<int> GetEffectiveCapacityAsync(
        Guid dormitoryId, Guid facultyId, Gender gender, Guid allocationPeriodId, CancellationToken cancellationToken = default);

    Task<int> GetUsedCapacityAsync(
        Guid dormitoryId, Guid facultyId, Gender gender, Guid allocationPeriodId, CancellationToken cancellationToken = default);

    Task<int> GetAvailableCapacityAsync(
        Guid dormitoryId, Guid facultyId, Gender gender, Guid allocationPeriodId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all Pending allocations across periods where AllocatedAt + ResponseWindowDays days <= now.
    /// Used by the time-based expiration worker.
    /// </summary>
    Task<List<DormAllocation>> GetExpirablePendingAsync(DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads all Pending allocations for a specific period whose Round is strictly less than the given round.
    /// Used by the admin "force-advance" path to collapse the response window.
    /// </summary>
    Task<List<DormAllocation>> GetPendingFromPriorRoundsAsync(Guid allocationPeriodId, int currentRound, CancellationToken cancellationToken = default);

    Task AddAsync(DormAllocation allocation, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
