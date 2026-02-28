using Fair.Application.Trips.Queries;
using Fair.Domain.Trips;

namespace Fair.Application.Trips.Guards;

public sealed class ActiveTripGuard
{
    private readonly ITripReadRepository _reads;

    public ActiveTripGuard(ITripReadRepository reads)
    {
        _reads = reads;
    }

    public async Task EnsureRiderHasNoOtherActiveTripAsync(Guid riderId, Guid currentTripId, CancellationToken ct)
    {
        var trips = await _reads.GetByRiderAsync(riderId, ct);

        var hasOtherActive = trips.Any(t =>
            t.Id != currentTripId &&
            IsActiveStatus(t.Status));

        if (hasOtherActive)
            throw new InvalidOperationException("rider_already_has_active_trip");
    }

    private static bool IsActiveStatus(string status) =>
        status is
            nameof(TripStatus.Draft) or
            nameof(TripStatus.Quoted) or
            nameof(TripStatus.Requested) or
            nameof(TripStatus.Accepted) or
            nameof(TripStatus.Arriving) or
            nameof(TripStatus.InProgress);
}