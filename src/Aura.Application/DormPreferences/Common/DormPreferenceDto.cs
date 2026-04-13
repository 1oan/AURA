namespace Aura.Application.DormPreferences.Common;

public record DormPreferenceDto(
    Guid DormitoryId,
    string DormitoryName,
    string CampusName,
    int Rank);
