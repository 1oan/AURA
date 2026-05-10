using Aura.Application.DormAllocations.Commands.AcceptAllocation;
using Aura.Application.DormAllocations.Commands.AdvanceRound;
using Aura.Application.DormAllocations.Commands.DeclineAllocation;
using Aura.Application.DormAllocations.Queries.GetAllocationsForPeriod;
using Aura.Application.DormAllocations.Queries.GetMyAllocation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/allocations")]
[Authorize]
public class DormAllocationsController(ISender sender) : ControllerBase
{
    [HttpGet("me/{allocationPeriodId:guid}")]
    public async Task<IActionResult> GetMine(Guid allocationPeriodId)
    {
        var result = await sender.Send(new GetMyAllocationQuery(allocationPeriodId));
        return Ok(result);
    }

    [Authorize(Roles = "FacultyAdmin,SuperAdmin")]
    [HttpGet("periods/{periodId:guid}")]
    public async Task<IActionResult> GetForPeriod(Guid periodId)
    {
        var result = await sender.Send(new GetAllocationsForPeriodQuery(periodId));
        return Ok(result);
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("periods/{periodId:guid}/advance-round")]
    public async Task<IActionResult> AdvanceRound(Guid periodId)
    {
        await sender.Send(new AdvanceRoundCommand(periodId));
        return NoContent();
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id)
    {
        await sender.Send(new AcceptAllocationCommand(id));
        return NoContent();
    }

    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id)
    {
        await sender.Send(new DeclineAllocationCommand(id));
        return NoContent();
    }
}
