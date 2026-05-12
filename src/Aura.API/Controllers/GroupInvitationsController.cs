using Aura.Application.RoommateGroups.Commands.AcceptGroupInvitation;
using Aura.Application.RoommateGroups.Commands.DeclineGroupInvitation;
using Aura.Application.RoommateGroups.Common;
using Aura.Application.RoommateGroups.Queries.GetMyInvitations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/group-invitations")]
[Authorize(Roles = "Student")]
public class GroupInvitationsController(ISender sender) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<List<InvitationDto>>> GetMine()
        => Ok(await sender.Send(new GetMyInvitationsQuery()));

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id)
    {
        await sender.Send(new AcceptGroupInvitationCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id)
    {
        await sender.Send(new DeclineGroupInvitationCommand(id));
        return NoContent();
    }
}
