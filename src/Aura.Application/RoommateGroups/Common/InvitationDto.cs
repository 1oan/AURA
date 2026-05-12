namespace Aura.Application.RoommateGroups.Common;

public record InvitationDto(
    Guid Id,
    Guid GroupId,
    string DormitoryName,
    int RoomSizePreference,
    Guid InviterUserId,
    string InviterFirstName,
    string InviterLastName,
    DateTime CreatedAt);
