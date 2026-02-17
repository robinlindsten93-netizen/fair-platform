using Fair.Application.Trips;
using Fair.Domain.Trips;

namespace Fair.Application.Trips.CompleteTrip;

public sealed class CompleteTripHandler
{
    private readonly ITripRepository _repo;

    public CompleteTripHandler(ITripRepository repo)
    {
        _repo = repo;
    }

    public async Task<CompleteTripResult> HandleAsync(
        CompleteTripRequest request,
        CancellationToken ct = default)
    {
        if (request.TripId == Guid.Empty)
            throw new ArgumentException("TripId is required.", nameof(request.TripId));

        var trip = await _repo.GetByIdAsync(request.TripId, ct);
        if (trip is null)
            throw new KeyNotFoundException("trip_not_found");

        // Idempotens: redan klar → returnera direkt
        if (trip.Status == TripStatus.Completed)
            return new CompleteTripResult(trip.Id, trip.Status);

        // Domain rule: måste vara InProgress
        trip.Complete(DateTimeOffset.UtcNow);

        // 🔒 Optimistic concurrency
        var ok = await _repo.UpdateAsync(trip, trip.Version, ct);
        if (!ok)
            throw new InvalidOperationException("concurrency_conflict");

        return new CompleteTripResult(trip.Id, trip.Status);
    }
}
