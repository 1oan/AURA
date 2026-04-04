using Aura.Application.Auth.Commands.ConfirmEmail;
using Aura.Application.Auth.Commands.Login;
using Aura.Application.Auth.Commands.Register;
using Aura.Application.Auth.Commands.ResendConfirmation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUserCommand command)
    {
        var result = await sender.Send(command);
        return Created(string.Empty, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginCommand command)
    {
        var result = await sender.Send(command);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailCommand command)
    {
        await sender.Send(command);
        return NoContent();
    }

    [Authorize]
    [HttpPost("resend-confirmation")]
    public async Task<IActionResult> ResendConfirmation()
    {
        await sender.Send(new ResendConfirmationCommand());
        return NoContent();
    }
}
