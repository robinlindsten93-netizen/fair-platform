using Fair.Application.Dispatch;
using Fair.Application.Trips;
using Fair.Domain.Trips;

namespace Fair.Application.Trips.CompleteTrip;

public sealed class CompleteTripHandler
{
    private readonly ITripRepository _repo;
    private readonly IDriverAssignmentRepository _assignments;

    public CompleteTripHandler(ITripRepository repo, IDriverAssignmentRepository assignments)
    {
        _repo = repo;
        _assignments = assignments;
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

        // Domänregel: måste vara InProgress (Trip.Complete kastar annars)
        trip.Complete(DateTimeOffset.UtcNow);

        // 🔒 Optimistic concurrency (viktigt: expectedVersion ska vara versionen vi läste)
        var expectedVersion = trip.Version;
        var ok = await _repo.UpdateAsync(trip, expectedVersion, ct);
        if (!ok)
            throw new InvalidOperationException("concurrency_conflict");

        // 🔓 Release driver busy (om driver finns)
        if (trip.DriverId.HasValue && trip.DriverId.Value != Guid.Empty)
        {
            await _assignments.ReleaseAsync(trip.DriverId.Value, trip.Id, ct);
        }

        return new CompleteTripResult(trip.Id, trip.Status);
    }
}
