using Fair.Application.Trips;
using Fair.Domain.Trips;
using System.Security.Claims;

namespace Fair.Application.Dispatch;

public sealed class AcceptDispatchOffer
{
    private readonly IDispatchOfferRepository _offers;
    private readonly ITripRepository _trips;
    private readonly IDriverAssignmentRepository _assignments;
    private readonly ITripNotifier _tripNotifier;

    public AcceptDispatchOffer(
        IDispatchOfferRepository offers,
        ITripRepository trips,
        IDriverAssignmentRepository assignments,
        ITripNotifier tripNotifier)
    {
        _offers = offers;
        _trips = trips;
        _assignments = assignments;
        _tripNotifier = tripNotifier;
    }

    public async Task<bool> Handle(ClaimsPrincipal user, Guid offerId, CancellationToken ct)
    {
        var driverId = GetDriverIdOrThrow(user);
        var now = DateTimeOffset.UtcNow;

        var offer = await _offers.GetByIdAsync(offerId, ct);
        if (offer is null)
            return false;

        var trip = await _trips.GetByIdAsync(offer.TripId, ct);
        if (trip is null)
            return false;

        if (trip.Status == TripStatus.Accepted && trip.DriverId == driverId)
            return true;

        if (trip.Status != TripStatus.Requested)
            return false;

        if (trip.Version != offer.TripVersion)
            return false;

        var assign = await _assignments.TryAssignAsync(driverId, offer.TripId, ct);
        if (assign == DriverAssignResult.AlreadyAssignedOtherTrip)
            return false;

        try
        {
            var accepted = await _offers.TryAcceptAsync(offerId, driverId, now, ct);
            if (!accepted)
            {
                if (await IsAlreadyAcceptedByMeAsync(driverId, offerId, offer.TripId, ct))
                    return true;

                await _assignments.ReleaseAsync(driverId, offer.TripId, ct);
                return false;
            }

            var expectedVersion = trip.Version;
            var vehicleId = "DEV_VEHICLE";

            trip.Accept(driverId, vehicleId, now);

            var updated = await _trips.UpdateAsync(trip, expectedVersion, ct);
            if (!updated)
            {
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

            var payload = new
            {
                tripId = trip.Id,
                riderId = trip.RiderId,
                driverId,
                status = trip.Status.ToString(),
                changedAtUtc = now
            };

            await _tripNotifier.NotifyRiderTripStatusChanged(trip.RiderId, payload, ct);
            await _tripNotifier.NotifyDriverTripStatusChanged(driverId, payload, ct);

            return true;
        }
        catch
        {
            await _assignments.ReleaseAsync(driverId, offer.TripId, ct);
            throw;
        }
    }

    private async Task<bool> IsAlreadyAcceptedByMeAsync(
        Guid driverId,
        Guid offerId,
        Guid tripId,
        CancellationToken ct)
    {
        var latestOffer = await _offers.GetByIdAsync(offerId, ct);
        var latestTrip = await _trips.GetByIdAsync(tripId, ct);

        return latestOffer is not null &&
               latestTrip is not null &&
               latestOffer.DriverId == driverId &&
               latestOffer.Status == "ACCEPTED" &&
               latestTrip.Status == TripStatus.Accepted &&
               latestTrip.DriverId == driverId;
    }

    private static Guid GetDriverIdOrThrow(ClaimsPrincipal user)
    {
        var sub =
            user.FindFirst("sub")?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            throw new InvalidOperationException("Missing user id claim (sub/nameidentifier).");

        if (!Guid.TryParse(sub, out var driverId) || driverId == Guid.Empty)
            throw new InvalidOperationException("Invalid user id claim (expected Guid).");

        return driverId;
    }
}