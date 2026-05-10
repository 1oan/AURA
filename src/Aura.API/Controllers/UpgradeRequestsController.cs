using Aura.Application.UpgradeRequests.Commands.CancelUpgradeRequest;
using Aura.Application.UpgradeRequests.Commands.SubmitUpgradeRequest;
using Aura.Application.UpgradeRequests.Queries.GetAvailableUpgradeTargets;
using Aura.Application.UpgradeRequests.Queries.GetMyUpgradeRequest;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/upgrade-requests")]
[Authorize]
public class UpgradeRequestsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit(SubmitUpgradeRequestRequest request)
    {
        var id = await sender.Send(new SubmitUpgradeRequestCommand(request.AllocationPeriodId, request.DormitoryIds));
        return Created($"/api/upgrade-requests/me/{request.AllocationPeriodId}", new { id });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await sender.Send(new CancelUpgradeRequestCommand(id));
        return NoContent();
    }

    [HttpGet("me/{periodId:guid}")]
    public async Task<IActionResult> GetMine(Guid periodId)
    {
        var result = await sender.Send(new GetMyUpgradeRequestQuery(periodId));
        return Ok(result);
    }

    [HttpGet("available-targets/{periodId:guid}")]
    public async Task<IActionResult> GetAvailableTargets(Guid periodId)
    {
        var result = await sender.Send(new GetAvailableUpgradeTargetsQuery(periodId));
        return Ok(result);
    }
}

public record SubmitUpgradeRequestRequest(Guid AllocationPeriodId, List<Guid> DormitoryIds);
