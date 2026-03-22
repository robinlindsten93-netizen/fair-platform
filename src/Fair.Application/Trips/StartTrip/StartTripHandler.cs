using Fair.Domain.Trips;

namespace Fair.Application.Trips.StartTrip;

public sealed class StartTripHandler
{
    private readonly ITripRepository _trips;
    private readonly ITripNotifier _tripNotifier;

    public StartTripHandler(
        ITripRepository trips,
        ITripNotifier tripNotifier)
    {
        _trips = trips;
        _tripNotifier = tripNotifier;
    }

    public async Task<StartTripResult> HandleAsync(StartTripRequest req, CancellationToken ct)
    {
        var trip = await _trips.GetByIdAsync(req.TripId, ct);
        if (trip is null)
            throw new KeyNotFoundException("trip_not_found");

        var expectedVersion = trip.Version;
        var now = DateTimeOffset.UtcNow;

        trip.Start(now);

        var updated = await _trips.UpdateAsync(trip, expectedVersion, ct);
        if (!updated)
            throw new InvalidOperationException("concurrency_conflict");

        if (trip.DriverId.HasValue)
        {
            var payload = new
            {
                tripId = trip.Id,
                riderId = trip.RiderId,
                driverId = trip.DriverId.Value,
                status = trip.Status.ToString(),
                changedAtUtc = now
            };

            await _tripNotifier.NotifyRiderTripStatusChanged(trip.RiderId, payload, ct);
            await _tripNotifier.NotifyDriverTripStatusChanged(trip.DriverId.Value, payload, ct);
        }

        return new StartTripResult(trip.Id, trip.Status);
    }
}