using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class StudentRecordRepository(AuraDbContext context) : IStudentRecordRepository
{
    public async Task<List<StudentRecord>> GetByPeriodAndFacultyAsync(
        Guid allocationPeriodId, Guid? facultyId, CancellationToken ct = default)
    {
        var query = context.StudentRecords
            .Include(sr => sr.Faculty)
            .Where(sr => sr.AllocationPeriodId == allocationPeriodId);

        if (facultyId.HasValue)
            query = query.Where(sr => sr.FacultyId == facultyId.Value);

        return await query
            .OrderByDescending(sr => sr.Points)
            .ToListAsync(ct);
    }

    public async Task<StudentRecord?> FindByMatriculationCodeAndPeriodAsync(
        string code, Guid allocationPeriodId, CancellationToken ct = default)
    {
        return await context.StudentRecords
            .Include(sr => sr.Faculty)
            .FirstOrDefaultAsync(sr => sr.MatriculationCode == code
                && sr.AllocationPeriodId == allocationPeriodId, ct);
    }

    public async Task DeleteByFacultyAndPeriodAsync(
        Guid facultyId, Guid allocationPeriodId, CancellationToken ct = default)
    {
        await context.StudentRecords
            .Where(sr => sr.FacultyId == facultyId && sr.AllocationPeriodId == allocationPeriodId)
            .ExecuteDeleteAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<StudentRecord> records, CancellationToken ct = default)
    {
        await context.StudentRecords.AddRangeAsync(records, ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await context.SaveChangesAsync(ct);
    }
}
