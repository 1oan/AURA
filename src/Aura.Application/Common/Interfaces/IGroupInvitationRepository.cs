using Aura.Domain.Entities;

namespace Aura.Application.Common.Interfaces;

public interface IGroupInvitationRepository
{
    Task<GroupInvitation?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GroupInvitation?> FindPendingAsync(Guid groupId, Guid inviteeUserId, CancellationToken cancellationToken = default);
    Task<List<GroupInvitation>> GetPendingForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<GroupInvitation>> GetPendingForGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task AddAsync(GroupInvitation invitation, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
