using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AuraDbContext _context;

    public UserRepository(AuraDbContext context)
    {
        _context = context;
    }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FindAsync([id], cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<List<User>> GetByIdsAsync(List<Guid> userIds, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<User>> SearchByNameForLobbyAsync(
        string queryFragment,
        Guid periodId,
        Guid dormitoryId,
        Gender gender,
        Guid excludeUserId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .Where(u => u.Id != excludeUserId && u.Gender == gender)
            .Where(u => _context.DormAllocations.Any(a =>
                a.UserId == u.Id &&
                a.AllocationPeriodId == periodId &&
                a.DormitoryId == dormitoryId &&
                a.Status == AllocationStatus.Accepted));

        // Empty fragment = no name filter (used for AI suggestions candidate pool)
        if (!string.IsNullOrWhiteSpace(queryFragment))
        {
            var pattern = $"%{queryFragment.Trim()}%";
            query = query.Where(u =>
                EF.Functions.ILike(u.FirstName + " " + u.LastName, pattern) ||
                EF.Functions.ILike(u.LastName + " " + u.FirstName, pattern));
        }

        return await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
