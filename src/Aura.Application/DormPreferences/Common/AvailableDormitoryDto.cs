namespace Aura.Application.DormPreferences.Common;

public record AvailableDormitoryDto(
    Guid DormitoryId,
    string DormitoryName,
    string CampusName,
    int AvailableSpots);
