using Aura.Application.Dormitories.Commands.CreateDormitory;
using Aura.Application.Dormitories.Commands.DeleteDormitory;
using Aura.Application.Dormitories.Commands.UpdateDormitory;
using Aura.Application.Dormitories.Queries.GetDormitories;
using Aura.Application.Dormitories.Queries.GetDormitoryById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/dormitories")]
[Authorize(Roles = "SuperAdmin")]
public class DormitoriesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid campusId)
        => Ok(await sender.Send(new GetDormitoriesQuery(campusId)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await sender.Send(new GetDormitoryByIdQuery(id)));

    [HttpPost]
    public async Task<IActionResult> Create(CreateDormitoryCommand command)
    {
        var result = await sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateDormitoryCommand command)
    {
        await sender.Send(command with { Id = id });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await sender.Send(new DeleteDormitoryCommand(id));
        return NoContent();
    }
}
