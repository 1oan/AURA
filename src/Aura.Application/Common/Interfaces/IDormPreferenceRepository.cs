using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IDormPreferenceRepository
{
    Task<List<DormPreference>> GetByUserAndPeriodAsync(Guid userId, Guid allocationPeriodId, CancellationToken cancellationToken = default);
    Task<int> CountByPeriodAndFacultyAsync(Guid allocationPeriodId, Guid facultyId, CancellationToken cancellationToken = default);
    Task<List<DormPreference>> GetByPeriodAndUsersAsync(Guid allocationPeriodId, List<Guid> userIds, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<DormPreference> preferences, CancellationToken cancellationToken = default);
    Task DeleteByUserAndPeriodAsync(Guid userId, Guid allocationPeriodId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
