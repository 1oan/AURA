using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IStudentRecordRepository
{
    Task<List<StudentRecord>> GetByPeriodAndFacultyAsync(Guid allocationPeriodId, Guid? facultyId, CancellationToken ct = default);
    Task<StudentRecord?> FindByMatriculationCodeAndPeriodAsync(string code, Guid allocationPeriodId, CancellationToken ct = default);
    Task DeleteByFacultyAndPeriodAsync(Guid facultyId, Guid allocationPeriodId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<StudentRecord> records, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
