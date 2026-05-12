using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class InterestRepository(AuraDbContext context) : IInterestRepository
{
    public async Task<List<Interest>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await context.Interests
            .Where(i => i.IsActive)
            .OrderBy(i => i.Category).ThenBy(i => i.DisplayOrder)
            .ToListAsync(cancellationToken);

    public async Task<List<Interest>> GetActiveBySlugsAsync(IEnumerable<string> slugs, CancellationToken cancellationToken = default)
    {
        var slugList = slugs.ToList();
        return await context.Interests
            .Where(i => i.IsActive && slugList.Contains(i.Slug))
            .ToListAsync(cancellationToken);
    }
}
