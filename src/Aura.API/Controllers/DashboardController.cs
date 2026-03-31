using Aura.Application.Dashboard.Queries.GetDashboardStats;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "SuperAdmin,FacultyAdmin")]
public class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
        => Ok(await sender.Send(new GetDashboardStatsQuery()));
}