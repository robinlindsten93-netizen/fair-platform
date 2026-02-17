using Fair.Application.Trips;
using Fair.Domain.Trips;

namespace Fair.Application.Trips.ArriveTrip;

public sealed class ArriveTripHandler
{
    private readonly ITripRepository _repo;

    public ArriveTripHandler(ITripRepository repo) => _repo = repo;

    public async Task<ArriveTripResult> HandleAsync(ArriveTripRequest request, CancellationToken ct = default)
    {
        if (request.TripId == Guid.Empty)
            throw new ArgumentException("TripId is required.", nameof(request.TripId));

        var trip = await _repo.GetByIdAsync(request.TripId, ct);
        if (trip is null) throw new KeyNotFoundException("trip_not_found");

        // Idempotens: om redan Arriving eller senare, returnera status
        if (trip.Status is TripStatus.Arriving or TripStatus.InProgress or TripStatus.Completed)
            return new ArriveTripResult(trip.Id, trip.Status);

        // Domain rule: måste vara Accepted
        trip.MarkArriving(DateTimeOffset.UtcNow);

        await _repo.UpdateAsync(trip, trip.Version, ct)
;
        return new ArriveTripResult(trip.Id, trip.Status);
    }
}
