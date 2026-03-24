using Aura.Application.Campuses.Commands.CreateCampus;
using Aura.Application.Campuses.Commands.DeleteCampus;
using Aura.Application.Campuses.Commands.UpdateCampus;
using Aura.Application.Campuses.Queries.GetCampusById;
using Aura.Application.Campuses.Queries.GetCampuses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/campuses")]
[Authorize(Roles = "SuperAdmin")]
public class CampusesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await sender.Send(new GetCampusesQuery()));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await sender.Send(new GetCampusByIdQuery(id)));

    [HttpPost]
    public async Task<IActionResult> Create(CreateCampusCommand command)
    {
        var result = await sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCampusCommand command)
    {
        await sender.Send(command with { Id = id });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await sender.Send(new DeleteCampusCommand(id));
        return NoContent();
    }
}
