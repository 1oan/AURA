using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Campus> Campuses { get; }
    DbSet<Dormitory> Dormitories { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Faculty> Faculties { get; }
    DbSet<AllocationPeriod> AllocationPeriods { get; }
    DbSet<FacultyRoomAllocation> FacultyRoomAllocations { get; }
    DbSet<DormAllocation> DormAllocations { get; }
    DbSet<UpgradeRequest> UpgradeRequests { get; }
    DbSet<UpgradeRequestTarget> UpgradeRequestTargets { get; }
    DbSet<DormPreference> DormPreferences { get; }
    DbSet<StudentRecord> StudentRecords { get; }
    DbSet<User> Users { get; }
}