using Fair.Application.Trips.Quoting;
using Fair.Domain.Trips;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/trips")]
public sealed class TripQuotesController : ControllerBase
{
    [HttpPost("quote")]
    [Authorize]
    public IActionResult Quote(
        [FromBody] QuoteBody body,
        [FromServices] ITripQuoteService quoteService,
        [FromServices] IQuoteTokenService tokenService)
    {
        var pickup = Location.Create(body.PickupLat, body.PickupLng);
        var dropoff = Location.Create(body.DropoffLat, body.DropoffLng);

        var quote = quoteService.CreateQuote(pickup, dropoff, body.Mode, DateTimeOffset.UtcNow);
        var quoteToken = tokenService.CreateToken(quote);

        return Ok(new
        {
            quote = new
            {
                estimatedDistanceMeters = quote.EstimatedDistanceMeters,
                estimatedDurationSeconds = quote.EstimatedDurationSeconds,
                price = new { amount = quote.Price.Amount, currency = quote.Price.Currency },
                expiresAtUtc = quote.ExpiresAtUtc,
                surgeMultiplier = quote.SurgeMultiplier
            },
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
