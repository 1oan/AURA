using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;

namespace Aura.Infrastructure.Persistence.Repositories;

public class SpotifySnapshotRepository(AuraDbContext context) : ISpotifySnapshotRepository
{
    public async Task AddAsync(SpotifySnapshot snapshot, CancellationToken cancellationToken = default)
        => await context.SpotifySnapshots.AddAsync(snapshot, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
