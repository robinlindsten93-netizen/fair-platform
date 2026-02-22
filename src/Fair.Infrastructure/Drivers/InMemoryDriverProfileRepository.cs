using Fair.Application.Dispatch;
using Fair.Application.Drivers;
using System.Collections.Concurrent;

namespace Fair.Infrastructure.Drivers;

public sealed class InMemoryDriverProfileRepository :
    IDriverProfileRepository,
    IDriverAvailabilityQuery
{
    private sealed record DriverProfile(bool IsOnline, DateTimeOffset LastSeenUtc);

    // KEY = userId (sub från JWT)
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

    // 🚨 KRITISK för dispatch
    public Task<IReadOnlyList<Guid>> GetOnlineDriverIdsAsync(CancellationToken ct)
    {
        // snapshot för thread-safety + debugging
        var snapshot = _store.ToArray();

        var online = snapshot
            .Where(kv => kv.Value.IsOnline)
            .Select(kv =>
            {
                if (Guid.TryParse(kv.Key, out var g))
                    return (Guid?)g;

                return null;
            })
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<Guid>>(online);
    }
}