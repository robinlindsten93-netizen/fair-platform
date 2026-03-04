using Fair.Application.Drivers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/driver/location")]
[Authorize(Policy = "Driver")]
public sealed class DriverLocationController : ControllerBase
{
    private readonly IDriverLocationWriter _writer;

    public DriverLocationController(IDriverLocationWriter writer)
    {
        _writer = writer;
    }

    public sealed record Body(double Lat, double Lng);

    [HttpPost]
    public async Task<IActionResult> Upsert([FromBody] Body body, CancellationToken ct)
    {
        var sub =
            User.FindFirstValue("sub") ??
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(sub, out var driverId))
            return Unauthorized();

        var location = new DriverLocationDto(
            driverId,
            body.Lat,
            body.Lng,
            DateTimeOffset.UtcNow
        );

        await _writer.UpsertAsync(location, ct);

        return Ok();
    }
}