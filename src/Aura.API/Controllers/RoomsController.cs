using Aura.Application.Rooms.Commands.BulkCreateRooms;
using Aura.Application.Rooms.Commands.CreateRoom;
using Aura.Application.Rooms.Commands.DeleteRoom;
using Aura.Application.Rooms.Commands.UpdateRoom;
using Aura.Application.Rooms.Queries.GetRoomById;
using Aura.Application.Rooms.Queries.GetRooms;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/rooms")]
[Authorize(Roles = "SuperAdmin")]
public class RoomsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid dormitoryId)
        => Ok(await sender.Send(new GetRoomsQuery(dormitoryId)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await sender.Send(new GetRoomByIdQuery(id)));

    [HttpPost]
    public async Task<IActionResult> Create(CreateRoomCommand command)
    {
        var result = await sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> BulkCreate(BulkCreateRoomsCommand command)
    {
        var count = await sender.Send(command);
        return Created(string.Empty, new { count });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateRoomCommand command)
    {
        if (id != command.Id)
            return BadRequest("Route and body IDs must match.");
        await sender.Send(command);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await sender.Send(new DeleteRoomCommand(id));
        return NoContent();
    }
}
