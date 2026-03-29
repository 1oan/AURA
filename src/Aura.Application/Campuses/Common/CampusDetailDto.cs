using Aura.Application.Dormitories.Common;

namespace Aura.Application.Campuses.Common;

public record CampusDetailDto(Guid Id, string Name, string? Address, List<DormitoryDto> Dormitories);
