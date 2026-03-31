namespace Aura.Application.Dashboard.Queries.GetDashboardStats;

public record DashboardStatsDto(
    int CampusCount,
    int DormitoryCount,
    int TotalRooms,
    int TotalCapacity,
    int FacultyCount,
    ActivePeriodDto? ActivePeriod,
    List<FacultyAllocationDto> AllocationsByFaculty);

public record ActivePeriodDto(
    Guid Id,
    string Name,
    string Status,
    DateTime StartDate,
    DateTime EndDate);

public record FacultyAllocationDto(
    Guid FacultyId,
    string FacultyName,
    string Abbreviation,
    int RoomCount);