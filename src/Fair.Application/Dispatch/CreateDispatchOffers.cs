using Fair.Application.Drivers;
using Fair.Application.Trips;
using Fair.Domain.Trips;
using Microsoft.Extensions.Logging;

namespace Fair.Application.Dispatch;

public sealed class CreateDispatchOffers
{
    private readonly ITripRepository _trips;
    private readonly IDispatchOfferRepository _offers;
    private readonly IDriverAssignmentRepository _assignments;
    private readonly IDriverAvailabilityQuery _availability;
    private readonly IDriverLocationQuery _locations;
    private readonly DispatchOptions _opt;
    private readonly IDispatchNotifier _notifier;
    private readonly ILogger<CreateDispatchOffers> _log;
    private readonly Action<Guid, int, DateTimeOffset>? _scheduleNextWave;

    public CreateDispatchOffers(
        ITripRepository trips,
        IDispatchOfferRepository offers,
        IDriverAssignmentRepository assignments,
        IDriverAvailabilityQuery availability,
        IDriverLocationQuery locations,
        DispatchOptions opt,
        IDispatchNotifier notifier,
        ILogger<CreateDispatchOffers> log,
        Action<Guid, int, DateTimeOffset>? scheduleNextWave = null)
    {
        _trips = trips;
        _offers = offers;
        _assignments = assignments;
        _availability = availability;
        _locations = locations;
        _opt = opt;
        _notifier = notifier;
        _log = log;
        _scheduleNextWave = scheduleNextWave;
    }

    public async Task Handle(Guid tripId, int tripVersion, CancellationToken ct)
    {
        _log.LogInformation("Dispatch.Handle start tripId={TripId} version={TripVersion}", tripId, tripVersion);

        var trip = await _trips.GetByIdAsync(tripId, ct);
        if (trip is null)
        {
            _log.LogWarning("Dispatch.Handle abort: trip not found tripId={TripId}", tripId);
            return;
        }

        if (trip.Status != TripStatus.Requested)
        {
            _log.LogWarning("Dispatch.Handle abort: trip status is {Status}, expected Requested. tripId={TripId}", trip.Status, tripId);
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var alreadyOffered = await _offers.GetOfferedDriverIdsAsync(tripId, tripVersion, ct);
        var totalAlready = alreadyOffered.Count;

        _log.LogInformation("Dispatch dedupe tripId={TripId} alreadyOffered={AlreadyOffered}", tripId, totalAlready);

        if (totalAlready >= _opt.MaxOffersPerTrip)
        {
            _log.LogInformation("Dispatch abort: max offers reached tripId={TripId}", tripId);
            return;
        }

        var isFirstWave = totalAlready == 0;
        var desiredThisWave = isFirstWave ? _opt.Wave1Count : _opt.WaveNCount;
        var remainingCapacity = _opt.MaxOffersPerTrip - totalAlready;
        var take = Math.Min(desiredThisWave, remainingCapacity);

        if (take <= 0)
        {
            _log.LogInformation("Dispatch abort: take <= 0 tripId={TripId}", tripId);
            return;
        }

        var onlineDriverIds = await _availability.GetOnlineDriverIdsAsync(ct);
        var onlineSet = onlineDriverIds.ToHashSet();

        _log.LogInformation("Dispatch online drivers count={Count}", onlineSet.Count);

        var maxAge = TimeSpan.FromSeconds(_opt.LocationMaxAgeSeconds);
        var candidates = await _locations.ListNearbyRecentAsync(
            centerLat: trip.Pickup.Latitude,
            centerLng: trip.Pickup.Longitude,
            radiusMeters: _opt.MaxSearchRadiusMeters,
            maxAge: maxAge,
            ct: ct);

        _log.LogInformation("Dispatch nearby recent candidates count={Count} tripId={TripId}", candidates.Count, tripId);

        if (candidates.Count == 0)
        {
            _log.LogWarning("Dispatch abort: no nearby candidates tripId={TripId}", tripId);
            return;
        }

        var ranked = new List<(Guid DriverId, double Score)>(candidates.Count);

        foreach (var loc in candidates)
        {
            if (!onlineSet.Contains(loc.DriverId))
            {
                _log.LogDebug("Dispatch skip driverId={DriverId}: not online", loc.DriverId);
                continue;
            }

            if (alreadyOffered.Contains(loc.DriverId))
            {
                _log.LogDebug("Dispatch skip driverId={DriverId}: already offered", loc.DriverId);
                continue;
            }

            if (await _assignments.IsBusyAsync(loc.DriverId, ct))
            {
                _log.LogDebug("Dispatch skip driverId={DriverId}: busy", loc.DriverId);
                continue;
            }

            var distance = Geo.DistanceMeters(
                trip.Pickup.Latitude,
                trip.Pickup.Longitude,
                loc.Lat,
                loc.Lng);

            var idleSeconds = (now - loc.RecordedAtUtc).TotalSeconds;
            var idleBonus = Math.Min(idleSeconds, 300);
            var jitter = Random.Shared.NextDouble() * 10;
            var score = distance - idleBonus + jitter;

            _log.LogInformation(
                "Dispatch rank driverId={DriverId} distance={Distance:F1} idleBonus={IdleBonus:F1} jitter={Jitter:F1} score={Score:F1}",
                loc.DriverId, distance, idleBonus, jitter, score);

            ranked.Add((loc.DriverId, score));
        }

        if (ranked.Count == 0)
        {
            _log.LogWarning("Dispatch abort: ranked list empty tripId={TripId}", tripId);
            return;
        }

        var selected = ranked
            .OrderBy(x => x.Score)
            .Take(take)
            .ToList();

        _log.LogInformation("Dispatch selected count={Count} tripId={TripId}", selected.Count, tripId);

        if (selected.Count == 0)
        {
            _log.LogWarning("Dispatch abort: selected list empty tripId={TripId}", tripId);
            return;
        }

        var ttl = TimeSpan.FromSeconds(_opt.OfferTtlSeconds);

        var newOffers = selected
            .Select(x => new DispatchOfferDto(
                OfferId: Guid.NewGuid(),
                TripId: trip.Id,
                DriverId: x.DriverId,
                TripVersion: tripVersion,
                CreatedAtUtc: now,
                ExpiresAtUtc: now.Add(ttl),
                Status: "PENDING"))
            .ToList();

        await _offers.AddManyAsync(newOffers, ct);

        _log.LogInformation("Dispatch created offers count={Count} tripId={TripId}", newOffers.Count, tripId);

        foreach (var offer in newOffers)
        {
            _log.LogInformation("Dispatch notifying driverId={DriverId} offerId={OfferId}", offer.DriverId, offer.OfferId);

            await _notifier.NotifyDriverNewOffer(
                offer.DriverId,
                new
                {
                    offer.OfferId,
                    offer.TripId,
                    offer.ExpiresAtUtc
                },
                ct);
        }

        var offeredAfter = totalAlready + newOffers.Count;

        if (offeredAfter < _opt.MaxOffersPerTrip)
        {
            var nextWaveAt = now.AddSeconds(_opt.WaveDelaySeconds);
            _log.LogInformation("Dispatch scheduling next wave tripId={TripId} nextWaveAt={NextWaveAt:o}", tripId, nextWaveAt);
            _scheduleNextWave?.Invoke(trip.Id, tripVersion, nextWaveAt);
        }
    }
}