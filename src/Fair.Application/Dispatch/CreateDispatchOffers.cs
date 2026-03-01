using Fair.Application.Drivers;
using Fair.Application.Trips;
using Fair.Domain.Trips;

namespace Fair.Application.Dispatch;

public sealed class CreateDispatchOffers
{
    private readonly ITripRepository _trips;
    private readonly IDispatchOfferRepository _offers;
    private readonly IDriverAssignmentRepository _assignments;
    private readonly IDriverAvailabilityQuery _availability;
    private readonly IDriverLocationQuery _locations;
    private readonly DispatchOptions _opt;

    private readonly Action<Guid, int, DateTimeOffset>? _scheduleNextWave;

    public CreateDispatchOffers(
        ITripRepository trips,
        IDispatchOfferRepository offers,
        IDriverAssignmentRepository assignments,
        IDriverAvailabilityQuery availability,
        IDriverLocationQuery locations,
        DispatchOptions opt,
        Action<Guid, int, DateTimeOffset>? scheduleNextWave = null)
    {
        _trips = trips;
        _offers = offers;
        _assignments = assignments;
        _availability = availability;
        _locations = locations;
        _opt = opt;
        _scheduleNextWave = scheduleNextWave;
    }

    public async Task Handle(Guid tripId, int tripVersion, CancellationToken ct)
    {
        var trip = await _trips.GetByIdAsync(tripId, ct);
        if (trip is null) return;

        if (trip.Status != TripStatus.Requested) return;

        var now = DateTimeOffset.UtcNow;

        // Dedupe per version
        var alreadyOffered = await _offers.GetOfferedDriverIdsAsync(tripId, tripVersion, ct);
        var totalAlready = alreadyOffered.Count;
        if (totalAlready >= _opt.MaxOffersPerTrip) return;

        var isFirstWave = totalAlready == 0;
        var desired = isFirstWave ? _opt.Wave1Count : _opt.WaveNCount;

        var remaining = _opt.MaxOffersPerTrip - totalAlready;
        var take = Math.Min(desired, remaining);
        if (take <= 0) return;

        // Fetch once
        var online = await _availability.GetOnlineDriverIdsAsync(ct);
        var onlineSet = online is HashSet<Guid> hs ? hs : online.ToHashSet();

        var maxAge = TimeSpan.FromSeconds(_opt.LocationMaxAgeSeconds);
        var candidates = await _locations.ListRecentAsync(maxAge, ct);

        var filtered = new List<(Guid DriverId, double DistMeters)>(candidates.Count);

        foreach (var loc in candidates)
        {
            if (alreadyOffered.Contains(loc.DriverId))
                continue;

            // Availability = online (v1)
            if (!onlineSet.Contains(loc.DriverId))
                continue;

            // Soft busy-filter (accept-flödet har hard busy via TryAssign)
            // Om du vill ha hard busy här: lägg till IsBusyAsync i IDriverAssignmentRepository.

            var d = Geo.DistanceMeters(
                trip.Pickup.Latitude, trip.Pickup.Longitude,
                loc.Lat, loc.Lng);

            if (d > _opt.MaxSearchRadiusMeters)
                continue;

            filtered.Add((loc.DriverId, d));
        }

        var top = filtered
            .OrderBy(x => x.DistMeters)
            .Take(take)
            .ToList();

        if (top.Count == 0) return;

        var ttl = TimeSpan.FromSeconds(_opt.OfferTtlSeconds);

        var newOffers = top.Select(x => new DispatchOfferDto(
            OfferId: Guid.NewGuid(),
            TripId: trip.Id,
            DriverId: x.DriverId,
            TripVersion: tripVersion,
            CreatedAtUtc: now,
            ExpiresAtUtc: now.Add(ttl),
            Status: "PENDING"
        )).ToList();

        await _offers.AddManyAsync(newOffers, ct);

        var offeredAfter = totalAlready + newOffers.Count;
        if (offeredAfter < _opt.MaxOffersPerTrip)
        {
            var next = now.AddSeconds(_opt.WaveDelaySeconds);
            _scheduleNextWave?.Invoke(trip.Id, tripVersion, next);
        }
    }
}