using Fair.Application.Dispatch;
using Fair.Domain.Trips;

namespace Fair.Application.Trips.CompleteTrip;

public sealed class CompleteTripHandler
{
    private readonly ITripRepository _trips;
    private readonly IDriverAssignmentRepository _assignments;
    private readonly ITripNotifier _tripNotifier;

    public CompleteTripHandler(
        ITripRepository trips,
        IDriverAssignmentRepository assignments,
        ITripNotifier tripNotifier)
    {
        _trips = trips;
        _assignments = assignments;
        _tripNotifier = tripNotifier;
    }

    public async Task<CompleteTripResult> HandleAsync(CompleteTripRequest req, CancellationToken ct)
    {
        var trip = await _trips.GetByIdAsync(req.TripId, ct);
        if (trip is null)
            throw new KeyNotFoundException("trip_not_found");

        var expectedVersion = trip.Version;
        var now = DateTimeOffset.UtcNow;

        trip.Complete(now);

        var updated = await _trips.UpdateAsync(trip, expectedVersion, ct);
        if (!updated)
            throw new InvalidOperationException("concurrency_conflict");

        if (trip.DriverId.HasValue)
        {
            await _assignments.ReleaseAsync(trip.DriverId.Value, trip.Id, ct);

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

        return new CompleteTripResult(trip.Id, trip.Status);
    }
}