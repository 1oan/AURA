using Aura.Domain.Entities;
using Aura.Domain.Enums;

namespace Aura.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<User>> GetByIdsAsync(List<Guid> userIds, CancellationToken cancellationToken = default);

    // Returns up to `limit` users matching the name fragment, filtered to Accepted allocations
    // in the given period/dorm/gender. Empty queryFragment skips the ILIKE filter.
    Task<List<User>> SearchByNameForLobbyAsync(
        string queryFragment,
        Guid periodId,
        Guid dormitoryId,
        Gender gender,
        Guid excludeUserId,
        int limit,
        CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
