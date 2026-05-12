using Aura.Application.Common.Interfaces;
using Aura.Domain.Entities;
using Aura.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Aura.Infrastructure.Persistence.Repositories;

public class GroupInvitationRepository(AuraDbContext context) : IGroupInvitationRepository
{
    public async Task<GroupInvitation?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await context.GroupInvitations.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<GroupInvitation?> FindPendingAsync(Guid groupId, Guid inviteeUserId, CancellationToken cancellationToken = default)
        => await context.GroupInvitations
            .FirstOrDefaultAsync(i => i.GroupId == groupId && i.InviteeUserId == inviteeUserId && i.Status == InvitationStatus.Pending, cancellationToken);

    public async Task<List<GroupInvitation>> GetPendingForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await context.GroupInvitations
            .Where(i => i.InviteeUserId == userId && i.Status == InvitationStatus.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<List<GroupInvitation>> GetPendingForGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await context.GroupInvitations
            .Where(i => i.GroupId == groupId && i.Status == InvitationStatus.Pending)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(GroupInvitation invitation, CancellationToken cancellationToken = default)
        => await context.GroupInvitations.AddAsync(invitation, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);
}
