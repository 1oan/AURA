using MediatR;

namespace Aura.Application.Common.Events;

public record GroupInvitationAcceptedEvent(
    Guid InvitationId,
    Guid GroupId,
    Guid InviteeUserId) : INotification;
