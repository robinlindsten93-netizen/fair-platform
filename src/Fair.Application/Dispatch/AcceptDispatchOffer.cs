using Fair.Application.Trips;
using Fair.Domain.Trips;
using System.Security.Claims;

namespace Fair.Application.Dispatch;

public sealed class AcceptDispatchOffer
{
    private readonly IDispatchOfferRepository _offers;
    private readonly ITripRepository _trips;
    private readonly IDriverAssignmentRepository _assignments;

    public AcceptDispatchOffer(
        IDispatchOfferRepository offers,
        ITripRepository trips,
        IDriverAssignmentRepository assignments)
    {
        _offers = offers;
        _trips = trips;
        _assignments = assignments;
    }

    public async Task<bool> Handle(ClaimsPrincipal user, Guid offerId, CancellationToken ct)
    {
        var sub =
            user.FindFirst("sub")?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            throw new InvalidOperationException("Missing user id claim (sub/nameidentifier).");

        if (!Guid.TryParse(sub, out var driverId) || driverId == Guid.Empty)
            throw new InvalidOperationException("Invalid user id claim (expected Guid).");

        var now = DateTimeOffset.UtcNow;

        // 1) Läs offer
        var offer = await _offers.GetByIdAsync(offerId, ct);
        if (offer is null) return false;

        // 2) Läs trip (för status + version + idempotens)
        var trip = await _trips.GetByIdAsync(offer.TripId, ct);
        if (trip is null) return false;

        // ✅ Idempotens: trip redan accepterad av samma driver -> OK
        if (trip.Status == TripStatus.Accepted && trip.DriverId == driverId)
            return true;

        // Trip måste vara Requested för att accepteras
        if (trip.Status != TripStatus.Requested)
            return false;

        // ✅ Hardening: offer får inte vara stale (skapad för annan tripVersion)
        if (trip.Version != offer.TripVersion)
            return false;

        // 3) Busy-skydd (driver kan inte ta två trips samtidigt)
        var assign = await _assignments.TryAssignAsync(driverId, offer.TripId, ct);
        if (assign == DriverAssignResult.AlreadyAssignedOtherTrip)
            return false;

        // 4) Atomic accept i offer-store (single-winner)
        var accepted = await _offers.TryAcceptAsync(offerId, driverId, now, ct);
        if (!accepted)
        {
            // ✅ Idempotens: om vi redan accepterat tidigare -> return true
            var latestOffer = await _offers.GetByIdAsync(offerId, ct);
            var latestTrip = await _trips.GetByIdAsync(offer.TripId, ct);

            var weAlreadyWon =
                latestOffer is not null &&
                latestOffer.DriverId == driverId &&
                latestOffer.Status == "ACCEPTED" &&
                latestTrip is not null &&
                latestTrip.Status == TripStatus.Accepted &&
                latestTrip.DriverId == driverId;

            if (weAlreadyWon)
                return true;

            await _assignments.ReleaseAsync(driverId, offer.TripId, ct);
            return false;
        }

        // 5) Uppdatera trip med optimistic concurrency
        // (använd versionen från trip vi läste innan vi gjorde transition)
        var expectedVersion = trip.Version;

        // v1 placeholder tills vi har vehicles kopplade till driver/fleet
        var vehicleId = "DEV_VEHICLE";

        trip.Accept(driverId, vehicleId, now);

        var ok = await _trips.UpdateAsync(trip, expectedVersion, ct);
        if (!ok)
        {
            // ✅ Om någon hann acceptera (ev vi själva via race) -> kolla idempotent fallback
            var latestTrip = await _trips.GetByIdAsync(offer.TripId, ct);

            var alreadyAcceptedByMe =
                latestTrip is not null &&
                latestTrip.Status == TripStatus.Accepted &&
                latestTrip.DriverId == driverId;

            if (alreadyAcceptedByMe)
                return true;

            await _assignments.ReleaseAsync(driverId, offer.TripId, ct);
            return false;
        }

        return true;
    }
}