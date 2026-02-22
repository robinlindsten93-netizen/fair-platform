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

        // 2) Busy-skydd (driver kan inte ta två trips samtidigt)
        var assign = await _assignments.TryAssignAsync(driverId, offer.TripId, ct);
        if (assign == DriverAssignResult.AlreadyAssignedOtherTrip)
            return false;

        // 3) Atomic accept i offer-store (single-winner)
        var accepted = await _offers.TryAcceptAsync(offerId, driverId, now, ct);
        if (!accepted)
        {
            await _assignments.ReleaseAsync(driverId, offer.TripId, ct);
            return false;
        }

        // 4) Uppdatera trip med optimistic concurrency
        var trip = await _trips.GetByIdAsync(offer.TripId, ct);
        if (trip is null)
        {
            await _assignments.ReleaseAsync(driverId, offer.TripId, ct);
            return false;
        }

        // v1 placeholder tills vi har vehicles kopplade till driver/fleet
        var vehicleId = "DEV_VEHICLE";

        trip.Accept(driverId, vehicleId, now);

        var ok = await _trips.UpdateAsync(trip, offer.TripVersion, ct);
        if (!ok)
        {
            await _assignments.ReleaseAsync(driverId, offer.TripId, ct);
            return false;
        }

        return true;
    }
}
