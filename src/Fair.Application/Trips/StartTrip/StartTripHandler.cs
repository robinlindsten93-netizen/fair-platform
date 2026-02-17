using Fair.Application.Trips;
using Fair.Domain.Trips;

namespace Fair.Application.Trips.StartTrip;

public sealed class StartTripHandler
{
    private readonly ITripRepository _repo;

    public StartTripHandler(ITripRepository repo)
    {
        _repo = repo;
    }

    public async Task<StartTripResult> HandleAsync(
        StartTripRequest request,
        CancellationToken ct = default)
    {
        if (request.TripId == Guid.Empty)
            throw new ArgumentException("TripId is required.", nameof(request.TripId));

        var trip = await _repo.GetByIdAsync(request.TripId, ct);
        if (trip is null)
            throw new KeyNotFoundException("trip_not_found");

        // Idempotens: redan startad eller klar → returnera nuvarande status
        if (trip.Status is TripStatus.InProgress or TripStatus.Completed)
            return new StartTripResult(trip.Id, trip.Status);

        // Domain rule: måste vara Accepted eller Arriving
        trip.Start(DateTimeOffset.UtcNow);

        // Optimistic concurrency
        var ok = await _repo.UpdateAsync(trip, trip.Version, ct);
        if (!ok)
            throw new InvalidOperationException("concurrency_conflict");

        return new StartTripResult(trip.Id, trip.Status);
    }
}
