using Fair.Application.Drivers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/driver/me")]
[Authorize]
public sealed class DriverMeController : ControllerBase
{
    // =========================
    // GET /api/v1/driver/me
    // =========================
    [HttpGet]
    [Authorize(Policy = "Driver")]
    [ProducesResponseType(typeof(DriverMeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DriverMeDto>> Get(
        [FromServices] GetDriverMe uc,
        CancellationToken ct)
    {
        var dto = await uc.Handle(User, ct);
        return Ok(dto);
    }

    // =========================
    // POST /api/v1/driver/me/availability
    // =========================
    public sealed record AvailabilityRequest(bool IsOnline);

    [HttpPost("availability")]
    [Authorize(Policy = "Driver")]
    [ProducesResponseType(typeof(DriverMeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DriverMeDto>> SetAvailability(
        [FromServices] SetDriverAvailability uc,
        [FromBody] AvailabilityRequest req,
        CancellationToken ct)
    {
        var dto = await uc.Handle(User, req.IsOnline, ct);
        return Ok(dto);
    }
}
