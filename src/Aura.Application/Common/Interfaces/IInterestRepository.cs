using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IInterestRepository
{
    Task<List<Interest>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<List<Interest>> GetActiveBySlugsAsync(IEnumerable<string> slugs, CancellationToken cancellationToken = default);
}
