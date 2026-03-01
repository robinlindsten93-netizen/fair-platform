using Fair.Application.Dispatch;
using Fair.Application.Drivers;
using System.Collections.Concurrent;

namespace Fair.Infrastructure.Drivers;

public sealed class InMemoryDriverProfileRepository :
    IDriverProfileRepository,
    IDriverAvailabilityQuery
{
    private sealed record DriverProfile(bool IsOnline, DateTimeOffset LastSeenUtc);

    // KEY = userId (sub från JWT) — string representation of Guid in dev
    private readonly ConcurrentDictionary<string, DriverProfile> _store = new();

    public Task<DriverMeDto> GetAsync(string userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var profile = _store.GetOrAdd(
            userId,
            _ => new DriverProfile(false, now));

        return Task.FromResult(new DriverMeDto(
            userId,
            profile.IsOnline,
            profile.LastSeenUtc));
    }

    public Task<DriverMeDto> SetAvailabilityAsync(string userId, bool isOnline, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        _store.AddOrUpdate(
            userId,
            _ => new DriverProfile(isOnline, now),
            (_, existing) => existing with
            {
                IsOnline = isOnline,
                LastSeenUtc = now
            });

        var updated = _store[userId];

        return Task.FromResult(new DriverMeDto(
            userId,
            updated.IsOnline,
            updated.LastSeenUtc));
    }

    // 🚨 KRITISK för dispatch (bulk)
    public Task<IReadOnlyList<Guid>> GetOnlineDriverIdsAsync(CancellationToken ct)
    {
        var snapshot = _store.ToArray();

        var online = snapshot
            .Where(kv => kv.Value.IsOnline)
            .Select(kv => Guid.TryParse(kv.Key, out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<Guid>>(online);
    }

    // ✅ NEW: snabb "point lookup" (guards/filters)
    public Task<bool> IsDriverOnlineAsync(Guid driverId, CancellationToken ct)
    {
        if (driverId == Guid.Empty)
            return Task.FromResult(false);

        var key = driverId.ToString();

        if (!_store.TryGetValue(key, out var profile))
            return Task.FromResult(false);

        return Task.FromResult(profile.IsOnline);
    }
}