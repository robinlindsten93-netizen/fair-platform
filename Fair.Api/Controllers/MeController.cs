using Fair.Application.Me;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly GetMe _getMe;

    public MeController(GetMe getMe)
    {
        _getMe = getMe;
    }

    [HttpGet]
    [ProducesResponseType(typeof(MeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MeDto>> Get(CancellationToken ct)
    {
        var dto = await _getMe.Handle(User, ct);
        return Ok(dto);
    }
}

