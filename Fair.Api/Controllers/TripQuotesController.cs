using Fair.Application.Trips.Quoting;
using Fair.Domain.Trips;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/trips/quotes")]
public sealed class TripQuotesController : ControllerBase
{
    [HttpPost]
    [Authorize]
    public IActionResult Quote(
        [FromBody] QuoteBody body,
        [FromServices] ITripQuoteService quoteService,
        [FromServices] IQuoteTokenService tokenService)
    {
        // Basic validation
        if (body.Mode < 0) return BadRequest(new { error = "invalid_mode" });

        Location pickup;
        Location dropoff;

        try
        {
            pickup = Location.Create(body.PickupLat, body.PickupLng);
            dropoff = Location.Create(body.DropoffLat, body.DropoffLng);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "invalid_location", detail = ex.Message });
        }

        if (pickup.Latitude == dropoff.Latitude && pickup.Longitude == dropoff.Longitude)
            return BadRequest(new { error = "pickup_equals_dropoff" });

        var nowUtc = DateTimeOffset.UtcNow;

        var quote = quoteService.CreateQuote(pickup, dropoff, body.Mode, nowUtc);
        var quoteToken = tokenService.CreateToken(quote);

        return Ok(new
        {
            estimatedDistanceMeters = quote.EstimatedDistanceMeters,
            estimatedDurationSeconds = quote.EstimatedDurationSeconds,
            price = new { amount = quote.Price.Amount, currency = quote.Price.Currency },
            expiresAtUtc = quote.ExpiresAtUtc,
            surgeMultiplier = quote.SurgeMultiplier,
            quoteToken
        });
    }

    public sealed record QuoteBody(
        double PickupLat,
        double PickupLng,
        double DropoffLat,
        double DropoffLng,
        TransportMode Mode);
}

