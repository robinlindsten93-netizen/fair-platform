using Fair.Application.Trips.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/trips")]
[Authorize]
public sealed class TripQueriesController : ControllerBase
{
    // =========================
    // LIST MY TRIPS (Rider)
    // =========================
    [HttpGet("my")]
    [Authorize(Policy = "Rider")]
    [ProducesResponseType(typeof(IReadOnlyList<TripReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TripReadDto>>> MyTrips(
        [FromServices] ITripReadRepository reads,
        CancellationToken ct)
    {
        var userId = GetUserIdOrThrow(User);
        var list = await reads.GetByRiderAsync(userId, ct);
        return Ok(list);
    }

    // =========================
    // LIST MY TRIPS (Driver)
    // =========================
    [HttpGet("driver/my")]
    [Authorize(Policy = "Driver")]
    [ProducesResponseType(typeof(IReadOnlyList<TripReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TripReadDto>>> MyTripsDriver(
        [FromServices] ITripReadRepository reads,
        CancellationToken ct)
    {
        var userId = GetUserIdOrThrow(User);
        var list = await reads.GetByDriverAsync(userId, ct);
        return Ok(list);
    }

    // =========================
    // GET MY ACTIVE TRIP (Rider)
    // =========================
    [HttpGet("my/active")]
    [Authorize(Policy = "Rider")]
    [ProducesResponseType(typeof(TripReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MyActiveTrip(
        [FromServices] ITripReadRepository reads,
        CancellationToken ct)
    {
        var userId = GetUserIdOrThrow(User);
        var list = await reads.GetByRiderAsync(userId, ct);

        var active = list.FirstOrDefault(t => IsActiveStatus(t.Status));
        return active is null ? NoContent() : Ok(active);
    }

    // =========================
    // GET MY ACTIVE TRIP (Driver)
    // =========================
    [HttpGet("driver/my/active")]
    [Authorize(Policy = "Driver")]
    [ProducesResponseType(typeof(TripReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MyActiveTripDriver(
        [FromServices] ITripReadRepository reads,
        CancellationToken ct)
    {
        var userId = GetUserIdOrThrow(User);
        var list = await reads.GetByDriverAsync(userId, ct);

        var active = list.FirstOrDefault(t => IsActiveStatus(t.Status));
        return active is null ? NoContent() : Ok(active);
    }

    private static bool IsActiveStatus(string status)
        => status is "Draft"
            or "Quoted"
            or "Requested"
            or "Accepted"
            or "Arriving"
            or "InProgress";

    private static Guid GetUserIdOrThrow(ClaimsPrincipal user)
    {
        var sub =
            user.FindFirst("sub")?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(sub, out var id) && id != Guid.Empty)
            return id;

        throw new UnauthorizedAccessException("missing_or_invalid_sub");
    }
}