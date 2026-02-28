using Fair.Application.Trips;

namespace Fair.Application.Trips.AcceptTrip;

public sealed class AcceptTripHandler
{
    private readonly ITripRepository _repo;

    public AcceptTripHandler(ITripRepository repo)
    {
        _repo = repo;
    }

    public async Task<AcceptTripResult> HandleAsync(AcceptTripRequest request, CancellationToken ct)
    {
        var trip = await _repo.GetByIdAsync(request.TripId, ct);
        if (trip is null)
            throw new KeyNotFoundException("trip_not_found");

        // Domain rule: måste vara Requested
        trip.Accept(request.DriverId, request.VehicleId, DateTimeOffset.UtcNow);

        var expectedVersion = trip.Version;
        var ok = await _repo.UpdateAsync(trip, expectedVersion, ct);
        if (!ok)
            throw new InvalidOperationException("concurrency_conflict");

        return new AcceptTripResult(trip.Id, trip.Status);
    }
}