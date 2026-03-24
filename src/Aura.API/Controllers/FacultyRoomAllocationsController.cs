using Aura.Application.FacultyRoomAllocations.Commands.AssignRooms;
using Aura.Application.FacultyRoomAllocations.Commands.RemoveRoomAssignments;
using Aura.Application.FacultyRoomAllocations.Queries.GetFacultyRoomAllocations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/faculty-room-allocations")]
[Authorize(Roles = "SuperAdmin")]
public class FacultyRoomAllocationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid periodId, [FromQuery] Guid? facultyId = null)
        => Ok(await sender.Send(new GetFacultyRoomAllocationsQuery(periodId, facultyId)));

    [HttpPost]
    public async Task<IActionResult> Assign(AssignRoomsCommand command)
    {
        var count = await sender.Send(command);
        return Created(string.Empty, new { count });
    }

    [HttpPost("remove")]
    public async Task<IActionResult> Remove(RemoveRoomAssignmentsCommand command)
    {
        await sender.Send(command);
        return NoContent();
    }
}
