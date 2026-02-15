using Fair.Application.Trips.CreateTrip;
using Fair.Domain.Trips;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/trips")]
public class TripsController : ControllerBase
{
    private readonly CreateTripHandler _create;

    public TripsController(CreateTripHandler create) => _create = create;

    public sealed record CreateTripBody(
        double PickupLat,
        double PickupLng,
        double DropoffLat,
        double DropoffLng,
        TransportMode Mode);

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateTripBody body, CancellationToken ct)
    {
        // Temporärt i V1: RiderId från token-sub senare.
        // Just nu tar vi från auth token om möjligt, annars generate (för dev).
        var riderId = Guid.TryParse(User.FindFirst("sub")?.Value, out var sub)
            ? sub
            : Guid.NewGuid();

        var req = new CreateTripRequest(
            riderId,
            body.PickupLat,
            body.PickupLng,
            body.DropoffLat,
            body.DropoffLng,
            body.Mode
        );

        var result = await _create.HandleAsync(req, ct);
        return CreatedAtAction(nameof(GetById), new { tripId = result.TripId }, result);
    }

    [HttpGet("{tripId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid tripId, [FromServices] Fair.Application.Trips.ITripRepository repo, CancellationToken ct)
    {
        var trip = await repo.GetByIdAsync(tripId, ct);
        if (trip is null) return NotFound();

        return Ok(trip);
    }
}
