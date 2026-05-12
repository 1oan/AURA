using Aura.Application.Common.Settings;
using Aura.Application.Profile.Commands.DisconnectSpotify;
using Aura.Application.Profile.Commands.ExchangeSpotifyCode;
using Aura.Application.Profile.Commands.SetInterests;
using Aura.Application.Profile.Commands.SubmitLifestyle;
using Aura.Application.Profile.Commands.SubmitTipi;
using Aura.Application.Profile.Common;
using Aura.Application.Profile.Queries.GetMyProfile;
using Aura.Application.Profile.Queries.StartSpotifyOAuth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Aura.API.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController(ISender sender, IOptions<SpotifySettings> spotifySettings) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileDto>> GetMyProfile()
        => Ok(await sender.Send(new GetMyProfileQuery()));

    [HttpPut("lifestyle")]
    public async Task<IActionResult> SubmitLifestyle([FromBody] SubmitLifestyleCommand command)
    {
        await sender.Send(command);
        return NoContent();
    }

    [HttpPut("tipi")]
    public async Task<IActionResult> SubmitTipi([FromBody] SubmitTipiCommand command)
    {
        await sender.Send(command);
        return NoContent();
    }

    [HttpPut("interests")]
    public async Task<IActionResult> SetInterests([FromBody] SetInterestsCommand command)
    {
        await sender.Send(command);
        return NoContent();
    }

    [HttpGet("spotify/start")]
    public async Task<ActionResult<StartSpotifyOAuthResult>> StartSpotifyOAuth()
        => Ok(await sender.Send(new StartSpotifyOAuthQuery()));

    [HttpGet("spotify/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> SpotifyCallback([FromQuery] string code, [FromQuery] string state)
    {
        var settings = spotifySettings.Value;
        await sender.Send(new ExchangeSpotifyCodeCommand(code, settings.RedirectUri, state));
        return Redirect($"{settings.FrontendBaseUrl}/profile?spotify=connected");
    }

    [HttpPost("spotify/disconnect")]
    public async Task<IActionResult> DisconnectSpotify()
    {
        await sender.Send(new DisconnectSpotifyCommand());
        return NoContent();
    }
}
