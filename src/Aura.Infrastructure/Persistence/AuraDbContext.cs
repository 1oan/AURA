using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence;

public sealed class AuraDbContext : DbContext, IApplicationDbContext
{
    public AuraDbContext(DbContextOptions<AuraDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Campus> Campuses => Set<Campus>();
    public DbSet<Dormitory> Dormitories => Set<Dormitory>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<AllocationPeriod> AllocationPeriods => Set<AllocationPeriod>();
    public DbSet<FacultyRoomAllocation> FacultyRoomAllocations => Set<FacultyRoomAllocation>();
    public DbSet<StudentRecord> StudentRecords => Set<StudentRecord>();
    public DbSet<EmailConfirmationCode> EmailConfirmationCodes => Set<EmailConfirmationCode>();
    public DbSet<DormPreference> DormPreferences => Set<DormPreference>();
    public DbSet<DormAllocation> DormAllocations => Set<DormAllocation>();
    public DbSet<UpgradeRequest> UpgradeRequests => Set<UpgradeRequest>();
    public DbSet<UpgradeRequestTarget> UpgradeRequestTargets => Set<UpgradeRequestTarget>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuraDbContext).Assembly);
    }
}
