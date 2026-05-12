using MediatR;

namespace Aura.Application.Common.Events;

public record GroupInvitationCreatedEvent(
    Guid InvitationId,
    Guid InviteeUserId,
    Guid InviterUserId,
    Guid GroupId) : INotification;
