using Fair.Application.Dispatch;
using Fair.Application.Trips;
using Fair.Application.Trips.AcceptTrip;
using Fair.Application.Trips.ArriveTrip;
using Fair.Application.Trips.CompleteTrip;
using Fair.Application.Trips.CreateTrip;
using Fair.Application.Trips.Queries;
using Fair.Application.Trips.RequestTrip;
using Fair.Application.Trips.StartTrip;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/trips")]
[Authorize]
public sealed class TripsController : ControllerBase
{
    // =========================
    // CREATE TRIP (Draft -> Quoted)
    // =========================
    [HttpPost]
    [Authorize(Policy = "Rider")]
    public async Task<IActionResult> Create(
        [FromBody] CreateTripBody body,
        [FromServices] CreateTripHandler handler,
        CancellationToken ct)
    {
        var riderId = GetUserIdOrThrow(User);

        var req = new CreateTripRequest(
            RiderId: riderId,
            PickupLat: body.PickupLat,
            PickupLng: body.PickupLng,
            DropoffLat: body.DropoffLat,
            DropoffLng: body.DropoffLng,
            Mode: body.Mode,
            QuoteToken: body.QuoteToken
        );

        try
        {
            var result = await handler.HandleAsync(req, ct);
            return CreatedAtAction(nameof(GetById), new { tripId = result.TripId }, result);
        }
        catch (UnauthorizedAccessException ex) when (ex.Message == "invalid_quote_token")
        {
            return Unauthorized(new { error = "invalid_quote_token" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "quote_expired")
        {
            return BadRequest(new { error = "quote_expired" });
        }
        catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "Mode")
        {
            return BadRequest(new { error = "invalid_transport_mode" });
        }
    }

    // =========================
    // REQUEST TRIP
    // =========================
    [HttpPost("{tripId:guid}/request")]
    [Authorize(Policy = "Rider")]
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
        catch (InvalidOperationException ex) when (ex.Message == "trip_not_requestable")
        {
            return BadRequest(new { error = "trip_not_requestable" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "concurrency_conflict")
        {
            return BadRequest(new { error = "concurrency_conflict" });
        }
    }

    // =========================
    // ACCEPT (Driver — guarded)
    // =========================
    [HttpPost("{tripId:guid}/accept")]
    [Authorize(Policy = "Driver")]
    public async Task<IActionResult> Accept(
        [FromRoute] Guid tripId,
        [FromBody] AcceptTripBody body,
        [FromServices] AcceptTripHandler handler,
        CancellationToken ct)
    {
        try
        {
            var driverId = GetUserIdOrThrow(User);

            var req = new AcceptTripRequest(tripId, driverId, body.VehicleId);
            var result = await handler.HandleAsync(req, ct);

            return Ok(new { tripId = result.TripId, status = result.Status.ToString() });
        }
        catch (KeyNotFoundException ex) when (ex.Message == "trip_not_found")
        {
            return NotFound(new { error = "trip_not_found" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "driver_not_assigned_to_trip")
        {
            return BadRequest(new { error = "driver_not_assigned_to_trip" });
        }
        catch (InvalidOperationException ex) when (ex.Message == "concurrency_conflict")
        {
            return BadRequest(new { error = "concurrency_conflict" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
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
    [Authorize(Policy = "Driver")]
    public async Task<IActionResult> Arrive(
        [FromRoute] Guid tripId,
        [FromServices] ArriveTripHandler handler,
        CancellationToken ct)
    {
        try
        {
            var result = await handler.HandleAsync(new ArriveTripRequest(tripId), ct);
            return Ok(new { tripId = result.TripId, status = result.Status.ToString() });
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
    [Authorize(Policy = "Driver")]
    public async Task<IActionResult> Start(
        [FromRoute] Guid tripId,
        [FromServices] StartTripHandler handler,
        CancellationToken ct)
    {
        try
        {
            var result = await handler.HandleAsync(new StartTripRequest(tripId), ct);
            return Ok(new { tripId = result.TripId, status = result.Status.ToString() });
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
    [Authorize(Policy = "Driver")]
    public async Task<IActionResult> Complete(
        [FromRoute] Guid tripId,
        [FromServices] CompleteTripHandler handler,
        CancellationToken ct)
    {
        try
        {
            var result = await handler.HandleAsync(new CompleteTripRequest(tripId), ct);
            return Ok(new { tripId = result.TripId, status = result.Status.ToString() });
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
    // READ: MY TRIPS (Rider)
    // =========================
    [HttpGet("mine")]
    [Authorize(Policy = "Rider")]
    public async Task<IActionResult> GetMine(
        [FromServices] ITripRepository trips,
        [FromServices] ITripReadRepository? reads,
        CancellationToken ct)
    {
        var riderId = GetUserIdOrThrow(User);

        // Preferred: read-model
        if (reads is not null)
        {
            var list = await reads.GetByRiderAsync(riderId, ct);
            return Ok(list);
        }

        // Fallback: write store (in-memory only)
        if (trips is ITripListSource listSource)
        {
            var all = await listSource.ListAllAsync(ct);
            var list = all
                .Where(t => t.RiderId == riderId)
                .OrderByDescending(t => t.CreatedAtUtc)
                .Select(Fair.Api.Contracts.TripDto.FromDomain)
                .ToList();

            return Ok(list);
        }

        return Ok(Array.Empty<object>());
    }

    // =========================
    // READ: MY TRIPS (Driver)
    // =========================
    [HttpGet("driver/mine")]
    [Authorize(Policy = "Driver")]
    public async Task<IActionResult> GetDriverMine(
        [FromServices] ITripRepository trips,
        [FromServices] ITripReadRepository? reads,
        CancellationToken ct)
    {
        var driverId = GetUserIdOrThrow(User);

        // Preferred: read-model
        if (reads is not null)
        {
            var list = await reads.GetByDriverAsync(driverId, ct);
            return Ok(list);
        }

        // Fallback: write store (in-memory only)
        if (trips is ITripListSource listSource)
        {
            var all = await listSource.ListAllAsync(ct);
            var list = all
                .Where(t => t.DriverId.HasValue && t.DriverId.Value == driverId)
                .OrderByDescending(t => t.CreatedAtUtc)
                .Select(Fair.Api.Contracts.TripDto.FromDomain)
                .ToList();

            return Ok(list);
        }

        return Ok(Array.Empty<object>());
    }

    // =========================
    // GET BY ID
    // =========================
    [HttpGet("{tripId:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid tripId,
        [FromServices] ITripRepository repo,
        CancellationToken ct)
    {
        var trip = await repo.GetByIdAsync(tripId, ct);
        if (trip is null) return NotFound(new { error = "trip_not_found" });

        return Ok(Fair.Api.Contracts.TripDto.FromDomain(trip));
    }

    // =========================
    // BODY RECORDS
    // =========================
    public sealed record CreateTripBody(
        double PickupLat,
        double PickupLng,
        double DropoffLat,
        double DropoffLng,
        int Mode,
        string QuoteToken);

    public sealed record RequestTripBody(string QuoteToken);

    public sealed record AcceptTripBody(string VehicleId);

    private static Guid GetUserIdOrThrow(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(sub, out var id)) return id;

        throw new UnauthorizedAccessException("missing_or_invalid_sub");
    }
}