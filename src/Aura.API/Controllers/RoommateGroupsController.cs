using Aura.Application.RoommateGroups.Commands.ChangeRoomSizePreference;
using Aura.Application.RoommateGroups.Commands.CreateRoommateGroup;
using Aura.Application.RoommateGroups.Commands.DisbandGroup;
using Aura.Application.RoommateGroups.Commands.InviteToGroup;
using Aura.Application.RoommateGroups.Commands.LeaveGroup;
using Aura.Application.RoommateGroups.Commands.LockGroup;
using Aura.Application.RoommateGroups.Common;
using Aura.Application.RoommateGroups.Queries.GetCompatibleSuggestions;
using Aura.Application.RoommateGroups.Queries.GetMyGroup;
using Aura.Application.RoommateGroups.Queries.SearchEligibleStudents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/groups")]
[Authorize(Roles = "Student")]
public class RoommateGroupsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateRoommateGroupCommand command)
    {
        var id = await sender.Send(command);
        return CreatedAtAction(nameof(GetMine), new { }, new { id });
    }

    [HttpGet("me")]
    public async Task<ActionResult<GroupDto?>> GetMine()
        => Ok(await sender.Send(new GetMyGroupQuery()));

    [HttpPost("{id:guid}/invite")]
    public async Task<IActionResult> Invite(Guid id, [FromBody] InviteToGroupRequest body)
    {
        await sender.Send(new InviteToGroupCommand(id, body.InviteeUserId));
        return NoContent();
    }

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id)
    {
        await sender.Send(new LeaveGroupCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/disband")]
    public async Task<IActionResult> Disband(Guid id)
    {
        await sender.Send(new DisbandGroupCommand(id));
        return NoContent();
    }

    [HttpPatch("{id:guid}/preference")]
    public async Task<IActionResult> ChangePreference(Guid id, [FromBody] ChangeRoomSizePreferenceRequest body)
    {
        await sender.Send(new ChangeRoomSizePreferenceCommand(id, body.NewPreference));
        return NoContent();
    }

    [HttpPost("{id:guid}/lock")]
    public async Task<IActionResult> Lock(Guid id)
    {
        await sender.Send(new LockGroupCommand(id));
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<EligibleStudentDto>>> Search([FromQuery] string q, [FromQuery] Guid periodId)
        => Ok(await sender.Send(new SearchEligibleStudentsQuery(q, periodId)));

    [HttpGet("{id:guid}/suggestions")]
    public async Task<ActionResult<List<CompatibilityCandidateDto>>> Suggestions(Guid id)
        => Ok(await sender.Send(new GetCompatibleSuggestionsQuery(id)));

    public record InviteToGroupRequest(Guid InviteeUserId);
    public record ChangeRoomSizePreferenceRequest(Aura.Domain.Enums.RoomSizePreference NewPreference);
}
