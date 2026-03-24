using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IFacultyRoomAllocationRepository
{
    Task<List<FacultyRoomAllocation>> GetByPeriodAndFacultyAsync(Guid? periodId, Guid? facultyId, CancellationToken cancellationToken = default);
    Task<List<FacultyRoomAllocation>> GetByRoomAndPeriodAsync(Guid roomId, Guid periodId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<FacultyRoomAllocation> allocations, CancellationToken cancellationToken = default);
    void RemoveRange(IEnumerable<FacultyRoomAllocation> allocations);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
