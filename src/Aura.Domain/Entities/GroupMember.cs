namespace Aura.Domain.Entities;

public class GroupMember
{
    public Guid GroupId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private GroupMember() { }

    internal static GroupMember Create(Guid groupId, Guid userId)
    {
        return new GroupMember
        {
            GroupId = groupId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
        };
    }
}
