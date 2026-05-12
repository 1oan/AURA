using Aura.Application.Rooms.Commands.BulkCreateRooms;
using Aura.Application.Rooms.Commands.CreateRoom;
using Aura.Application.Rooms.Commands.DeleteRoom;
using Aura.Application.Rooms.Commands.UpdateRoom;
using Aura.Application.Rooms.Queries.GetRoomById;
using Aura.Application.Rooms.Queries.GetRooms;
using Aura.Application.RoomAssignments.Commands.PlaceMeNow;
using Aura.Application.RoomAssignments.Queries.GetActivePeriodCountdown;
using Aura.Application.RoomAssignments.Queries.GetMyRoom;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/rooms")]
public class RoomsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetAll([FromQuery] Guid dormitoryId)
        => Ok(await sender.Send(new GetRoomsQuery(dormitoryId)));

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await sender.Send(new GetRoomByIdQuery(id)));

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create(CreateRoomCommand command)
    {
        var result = await sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("bulk")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> BulkCreate(BulkCreateRoomsCommand command)
    {
        var count = await sender.Send(command);
        return Created(string.Empty, new { count });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Update(Guid id, UpdateRoomCommand command)
    {
        await sender.Send(command with { Id = id });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await sender.Send(new DeleteRoomCommand(id));
        return NoContent();
    }

    [HttpPost("place-me-now")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> PlaceMeNow()
    {
        await sender.Send(new PlaceMeNowCommand());
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<RoomDto?>> GetMyRoom()
        => Ok(await sender.Send(new GetMyRoomQuery()));

    [HttpGet("period-countdown")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<PeriodCountdownDto?>> GetPeriodCountdown()
        => Ok(await sender.Send(new GetActivePeriodCountdownQuery()));
}
