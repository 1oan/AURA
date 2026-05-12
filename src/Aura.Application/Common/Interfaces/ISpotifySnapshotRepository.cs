using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface ISpotifySnapshotRepository
{
    Task AddAsync(SpotifySnapshot snapshot, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
