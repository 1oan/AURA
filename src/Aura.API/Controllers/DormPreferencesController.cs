using Aura.Application.DormPreferences.Commands.SubmitPreferences;
using Aura.Application.DormPreferences.Queries.GetAvailableDormitories;
using Aura.Application.DormPreferences.Queries.GetMyPreferences;
using Aura.Application.DormPreferences.Queries.GetPreferenceStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/dorm-preferences")]
[Authorize]
public class DormPreferencesController(ISender sender) : ControllerBase
{
    [HttpGet("available/{allocationPeriodId:guid}")]
    public async Task<IActionResult> GetAvailableDormitories(Guid allocationPeriodId)
    {
        var result = await sender.Send(new GetAvailableDormitoriesQuery(allocationPeriodId));
        return Ok(result);
    }

    [HttpPut("{allocationPeriodId:guid}")]
    public async Task<IActionResult> SubmitPreferences(Guid allocationPeriodId, SubmitPreferencesRequest request)
    {
        await sender.Send(new SubmitPreferencesCommand(allocationPeriodId, request.DormitoryIds));
        return NoContent();
    }

    [HttpGet("my/{allocationPeriodId:guid}")]
    public async Task<IActionResult> GetMyPreferences(Guid allocationPeriodId)
    {
        var result = await sender.Send(new GetMyPreferencesQuery(allocationPeriodId));
        return Ok(result);
    }

    [Authorize(Roles = "SuperAdmin,FacultyAdmin")]
    [HttpGet("stats/{allocationPeriodId:guid}")]
    public async Task<IActionResult> GetPreferenceStats(Guid allocationPeriodId)
    {
        var result = await sender.Send(new GetPreferenceStatsQuery(allocationPeriodId));
        return Ok(result);
    }
}

public record SubmitPreferencesRequest(List<Guid> DormitoryIds);
