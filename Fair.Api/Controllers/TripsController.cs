using Fair.Application.Trips;
using Fair.Application.Trips.CreateTrip;
using Fair.Application.Trips.Quoting;
using Fair.Application.Trips.RequestTrip;
using Fair.Application.Trips.AcceptTrip;
using Fair.Application.Trips.ArriveTrip;
using Fair.Application.Trips.StartTrip;
using Fair.Application.Trips.CompleteTrip;
using Fair.Domain.Trips;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/trips")]
public sealed class TripsController : ControllerBase
{
    private readonly CreateTripHandler _create;

    public TripsController(CreateTripHandler create)
    {
        _create = create;
    }

    // =========================
    // BODY RECORDS
    // =========================

    public sealed record CreateTripBody(
        double PickupLat,
        double PickupLng,
        double DropoffLat,
        double DropoffLng,
        TransportMode Mode,
        string QuoteToken);

    public sealed record RequestTripBody(string QuoteToken);

    public sealed record AcceptTripBody(Guid DriverId, string VehicleId);

    // =========================
    // CREATE TRIP
    // =========================
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateTripBody body,
        [FromServices] IQuoteTokenService quoteTokenService,
        CancellationToken ct)
    {
        var riderId = Guid.TryParse(User.FindFirst("sub")?.Value, out var sub)
            ? sub
            : Guid.NewGuid();

        if (!quoteTokenService.TryParseToken(body.QuoteToken, out var quote))
            return Unauthorized(new { error = "invalid_quote_token" });

        if (quote.IsExpired(DateTimeOffset.UtcNow))
            return BadRequest(new { error = "quote_expired" });

        var req = new CreateTripRequest(
            riderId,
            body.PickupLat,
            body.PickupLng,
            body.DropoffLat,
            body.DropoffLng,
            body.Mode,
            quote);

        var result = await _create.HandleAsync(req, ct);

        return CreatedAtAction(nameof(GetById), new { tripId = result.TripId }, result);
    }

    // =========================
    // REQUEST TRIP
    // =========================
    [HttpPost("{tripId:guid}/request")]
    [Authorize]
    public async Task<IActionResult> RequestTrip(
        [FromRoute] Guid tripId,
        [FromBody] RequestTripBody body,
        [FromServices] RequestTripHandler handler,
        CancellationToken ct)
    {
        try
        {
            var req = new RequestTripRequest(tripId, body.QuoteToken);
            var result = await handler.HandleAsync(req, ct);

            return Ok(new
            {
                tripId = result.TripId,
                status = result.Status.ToString()
            });
        }
        catch (KeyNotFoundException ex) when (ex.Message == "trip_not_found")
        {
            return NotFound(new { error = "trip_not_found" });
        }
        catch (UnauthorizedAccessException ex) when (ex.Message == "invalid_quote_token")
        {
            return Unauthorized(new { error = "invalid_quote_token" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "quote_expired")
        {
            return BadRequest(new { error = "quote_expired" });
        }
    }

    // =========================
    // ACCEPT TRIP
    // =========================
    [HttpPost("{tripId:guid}/accept")]
    [Authorize]
    public async Task<IActionResult> Accept(
        [FromRoute] Guid tripId,
        [FromBody] AcceptTripBody body,
        [FromServices] AcceptTripHandler handler,
        CancellationToken ct)
    {
        try
        {
            var req = new AcceptTripRequest(tripId, body.DriverId, body.VehicleId);
            var result = await handler.HandleAsync(req, ct);

            return Ok(new
            {
                tripId = result.TripId,
                status = result.Status.ToString()
            });
        }
        catch (KeyNotFoundException ex) when (ex.Message == "trip_not_found")
        {
            return NotFound(new { error = "trip_not_found" });
        }
        catch (InvalidOperationException)
        {
            return BadRequest(new { error = "trip_not_requestable" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // =========================
    // ARRIVE
    // =========================
    [HttpPost("{tripId:guid}/arrive")]
    [Authorize]
    public async Task<IActionResult> Arrive(
        [FromRoute] Guid tripId,
        [FromServices] ArriveTripHandler handler,
        CancellationToken ct)
    {
        try
        {
            var req = new ArriveTripRequest(tripId);
            var result = await handler.HandleAsync(req, ct);

            return Ok(new
            {
                tripId = result.TripId,
                status = result.Status.ToString()
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "trip_not_found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // =========================
    // START
    // =========================
    [HttpPost("{tripId:guid}/start")]
    [Authorize]
    public async Task<IActionResult> Start(
        [FromRoute] Guid tripId,
        [FromServices] StartTripHandler handler,
        CancellationToken ct)
    {
        try
        {
            var req = new StartTripRequest(tripId);
            var result = await handler.HandleAsync(req, ct);

            return Ok(new
            {
                tripId = result.TripId,
                status = result.Status.ToString()
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "trip_not_found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // =========================
    // COMPLETE
    // =========================
    [HttpPost("{tripId:guid}/complete")]
    [Authorize]
    public async Task<IActionResult> Complete(
        [FromRoute] Guid tripId,
        [FromServices] CompleteTripHandler handler,
        CancellationToken ct)
    {
        try
        {
            var req = new CompleteTripRequest(tripId);
            var result = await handler.HandleAsync(req, ct);

            return Ok(new
            {
                tripId = result.TripId,
                status = result.Status.ToString()
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "trip_not_found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // =========================
    // GET BY ID
    // =========================
    [HttpGet("{tripId:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid tripId,
        [FromServices] ITripRepository repo,
        CancellationToken ct)
    {
        var trip = await repo.GetByIdAsync(tripId, ct);
        if (trip is null) return NotFound(new { error = "trip_not_found" });

        return Ok(Fair.Api.Contracts.TripDto.FromDomain(trip));
    }
}
