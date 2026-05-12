using Aura.Application.Profile.Common;
using Aura.Application.Profile.Queries.GetInterestCatalog;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/interests")]
[Authorize]
public class InterestsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<InterestCategoryDto>>> GetCatalog()
        => Ok(await sender.Send(new GetInterestCatalogQuery()));
}
