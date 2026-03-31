using Aura.Application.Users.Commands.ChangePassword;
using Aura.Application.Users.Commands.PromoteUser;
using Aura.Application.Users.Commands.SetMatriculationCode;
using Aura.Application.Users.Commands.UpdateProfile;
using Aura.Application.Users.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var result = await _sender.Send(new GetCurrentUserQuery());
        return Ok(result);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileCommand command)
    {
        await _sender.Send(command);
        return NoContent();
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordCommand command)
    {
        await _sender.Send(command);
        return NoContent();
    }

    [HttpPatch("me/matriculation-code")]
    public async Task<IActionResult> SetMatriculationCode(SetMatriculationCodeCommand command)
    {
        await _sender.Send(command);
        return NoContent();
    }

    [HttpPut("{userId:guid}/role")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> PromoteUser(Guid userId, PromoteUserCommand command)
    {
        if (userId != command.UserId)
            return BadRequest("Route and body user IDs must match.");

        await _sender.Send(command);
        return NoContent();
    }
}
