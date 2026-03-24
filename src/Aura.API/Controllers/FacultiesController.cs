using Aura.Application.Faculties.Commands.CreateFaculty;
using Aura.Application.Faculties.Commands.DeleteFaculty;
using Aura.Application.Faculties.Commands.UpdateFaculty;
using Aura.Application.Faculties.Queries.GetFaculties;
using Aura.Application.Faculties.Queries.GetFacultyById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/faculties")]
[Authorize(Roles = "SuperAdmin")]
public class FacultiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await sender.Send(new GetFacultiesQuery()));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await sender.Send(new GetFacultyByIdQuery(id)));

    [HttpPost]
    public async Task<IActionResult> Create(CreateFacultyCommand command)
    {
        var result = await sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateFacultyCommand command)
    {
        await sender.Send(command with { Id = id });
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await sender.Send(new DeleteFacultyCommand(id));
        return NoContent();
    }
}
