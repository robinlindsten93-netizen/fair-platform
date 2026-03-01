using Fair.Application.Dispatch;
using System.Collections.Concurrent;

namespace Fair.Infrastructure.Dispatch;

public sealed class InMemoryDispatchOfferRepository : IDispatchOfferRepository
{
    private sealed record Offer(
        Guid OfferId,
        Guid TripId,
        Guid DriverId,
        int TripVersion,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset ExpiresAtUtc,
        string Status // PENDING / ACCEPTED / EXPIRED
    );

    private readonly ConcurrentDictionary<Guid, Offer> _store = new();
    private readonly ConcurrentDictionary<Guid, object> _tripLocks = new();

    public Task AddManyAsync(IEnumerable<DispatchOfferDto> offers, CancellationToken ct)
    {
        foreach (var o in offers)
        {
            _store.TryAdd(o.OfferId, new Offer(
                o.OfferId, o.TripId, o.DriverId, o.TripVersion,
                o.CreatedAtUtc, o.ExpiresAtUtc, o.Status
            ));
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DispatchOfferDto>> GetPendingOffersForDriverAsync(Guid driverId, DateTimeOffset nowUtc, CancellationToken ct)
    {
        ExpireInternal(nowUtc);

        var list = _store.Values
            .Where(o => o.DriverId == driverId && o.Status == "PENDING" && o.ExpiresAtUtc > nowUtc)
            .OrderBy(o => o.CreatedAtUtc)
            .Select(ToDto)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<DispatchOfferDto>>(list);
    }

    public Task<DispatchOfferDto?> GetByIdAsync(Guid offerId, CancellationToken ct)
    {
        if (_store.TryGetValue(offerId, out var o))
            return Task.FromResult<DispatchOfferDto?>(ToDto(o));

        return Task.FromResult<DispatchOfferDto?>(null);
    }

    public Task<bool> ExistsForTripVersionAsync(Guid tripId, int tripVersion, CancellationToken ct)
    {
        var exists = _store.Values.Any(o => o.TripId == tripId && o.TripVersion == tripVersion);
        return Task.FromResult(exists);
    }

    public Task<IReadOnlySet<Guid>> GetOfferedDriverIdsAsync(Guid tripId, int tripVersion, CancellationToken ct)
    {
        var set = _store.Values
            .Where(o => o.TripId == tripId && o.TripVersion == tripVersion)
            .Select(o => o.DriverId)
            .ToHashSet();

        return Task.FromResult<IReadOnlySet<Guid>>(set);
    }

    public Task<bool> TryAcceptAsync(Guid offerId, Guid driverId, DateTimeOffset nowUtc, CancellationToken ct)
    {
        ExpireInternal(nowUtc);

        if (!_store.TryGetValue(offerId, out var existing))
            return Task.FromResult(false);

        var gate = _tripLocks.GetOrAdd(existing.TripId, _ => new object());

        lock (gate)
        {
            if (!_store.TryGetValue(offerId, out existing))
                return Task.FromResult(false);

            if (existing.DriverId != driverId)
                return Task.FromResult(false);

            if (existing.Status != "PENDING")
                return Task.FromResult(false);

            if (existing.ExpiresAtUtc <= nowUtc)
                return Task.FromResult(false);

            var tripAlreadyTaken = _store.Values.Any(o => o.TripId == existing.TripId && o.Status == "ACCEPTED");
            if (tripAlreadyTaken)
                return Task.FromResult(false);

            _store[offerId] = existing with { Status = "ACCEPTED" };

            foreach (var kv in _store)
            {
                var o = kv.Value;
                if (o.TripId == existing.TripId && o.OfferId != offerId && o.Status == "PENDING")
                {
                    _store.TryUpdate(kv.Key, o with { Status = "EXPIRED" }, o);
                }
            }

            return Task.FromResult(true);
        }
    }

    public Task ExpireOffersAsync(DateTimeOffset nowUtc, CancellationToken ct)
    {
        ExpireInternal(nowUtc);
        return Task.CompletedTask;
    }

    private void ExpireInternal(DateTimeOffset nowUtc)
    {
        foreach (var kv in _store)
        {
            var o = kv.Value;
            if (o.Status == "PENDING" && o.ExpiresAtUtc <= nowUtc)
            {
                _store.TryUpdate(kv.Key, o with { Status = "EXPIRED" }, o);
            }
        }
    }

    private static DispatchOfferDto ToDto(Offer o) => new(
        o.OfferId,
        o.TripId,
        o.DriverId,
        o.TripVersion,
        o.CreatedAtUtc,
        o.ExpiresAtUtc,
        o.Status
    );
}