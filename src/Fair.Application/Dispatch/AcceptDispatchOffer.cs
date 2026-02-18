using Fair.Application.Trips;
using Fair.Domain.Trips;
using System.Security.Claims;

namespace Fair.Application.Dispatch;

public sealed class AcceptDispatchOffer
{
    private readonly IDispatchOfferRepository _offers;
    private readonly ITripRepository _trips;

    public AcceptDispatchOffer(IDispatchOfferRepository offers, ITripRepository trips)
    {
        _offers = offers;
        _trips = trips;
    }

    public async Task<bool> Handle(ClaimsPrincipal user, Guid offerId, CancellationToken ct)
    {
        var driverId =
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            user.FindFirst("sub")?.Value ??
            throw new InvalidOperationException("Missing user id claim (sub/nameidentifier).");

        var now = DateTimeOffset.UtcNow;

        // 1) Läs offer
        var offer = await _offers.GetByIdAsync(offerId, ct);
        if (offer is null) return false;

        // 2) Försök acceptera offer atomiskt (förhindrar dubbel-accept)
        var accepted = await _offers.TryAcceptAsync(offerId, driverId, now, ct);
        if (!accepted) return false;

        // 3) Uppdatera trip med optimistic concurrency
  // 3) Uppdatera trip med optimistic concurrency
var trip = await _trips.GetByIdAsync(offer.TripId, ct);
if (trip is null) return false;

// v1 placeholder tills vi har riktiga vehicles kopplade till drivers/fleets
var vehicleId = Guid.Empty;

trip.Accept(vehicleId, driverId, now);

var ok = await _trips.UpdateAsync(trip, offer.TripVersion, ct);
if (!ok)
{
    return false;
}

return true;

    }
}
