using Aura.Application.AllocationPeriods.Commands.ActivateAllocationPeriod;
using Aura.Application.AllocationPeriods.Commands.CloseAllocationPeriod;
using Aura.Application.AllocationPeriods.Commands.StartAllocating;
using Aura.Application.AllocationPeriods.Commands.CreateAllocationPeriod;
using Aura.Application.AllocationPeriods.Commands.DeleteAllocationPeriod;
using Aura.Application.AllocationPeriods.Commands.UpdateAllocationPeriod;
using Aura.Application.AllocationPeriods.Queries.GetAllocationPeriodById;
using Aura.Application.AllocationPeriods.Queries.GetAllocationPeriods;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/allocation-periods")]
[Authorize]
public class AllocationPeriodsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await sender.Send(new GetAllocationPeriodsQuery()));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
        => Ok(await sender.Send(new GetAllocationPeriodByIdQuery(id)));

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Create(CreateAllocationPeriodCommand command)
    {
        var result = await sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Update(Guid id, UpdateAllocationPeriodCommand command)
    {
        await sender.Send(command with { Id = id });
        return NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Activate(Guid id)
    {
        await sender.Send(new ActivateAllocationPeriodCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/start-allocating")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> StartAllocating(Guid id)
    {
        await sender.Send(new StartAllocatingCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Close(Guid id)
    {
        await sender.Send(new CloseAllocationPeriodCommand(id));
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await sender.Send(new DeleteAllocationPeriodCommand(id));
        return NoContent();
    }
}
