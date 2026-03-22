using Fair.Api.Contracts.Driver;
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

    [HttpPost]
    public async Task<IActionResult> UpdateLocation(
        [FromBody] UpdateDriverLocationRequest req,
        CancellationToken ct)
    {
        var sub =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        if (!Guid.TryParse(sub, out var driverId))
            return Unauthorized();

        var location = new DriverLocationDto(
            DriverId: driverId,
            Lat: req.Lat,
            Lng: req.Lng,
            RecordedAtUtc: DateTimeOffset.UtcNow
        );

        await _writer.UpsertAsync(location, ct);

        return Ok();
    }
}