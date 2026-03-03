using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence;

public sealed class AuraDbContext : DbContext
{
    public AuraDbContext(DbContextOptions<AuraDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuraDbContext).Assembly);
    }
}
