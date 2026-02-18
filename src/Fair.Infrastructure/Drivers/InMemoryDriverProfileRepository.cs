using Fair.Application.Dispatch;
using Fair.Application.Drivers;
using System.Collections.Concurrent;

namespace Fair.Infrastructure.Drivers;

public sealed class InMemoryDriverProfileRepository :
    IDriverProfileRepository,
    IDriverAvailabilityQuery
{
    private sealed record DriverProfile(bool IsOnline, DateTimeOffset LastSeenUtc);

    private readonly ConcurrentDictionary<string, DriverProfile> _store = new();

    public Task<DriverMeDto> GetAsync(string userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var profile = _store.GetOrAdd(userId, _ => new DriverProfile(false, now));
        return Task.FromResult(new DriverMeDto(userId, profile.IsOnline, profile.LastSeenUtc));
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
            }
        );

        return GetAsync(userId, ct);
    }

    // 🔥 DISPATCH: lista online drivers
    public Task<IReadOnlyList<string>> GetOnlineDriverIdsAsync(CancellationToken ct)
    {
        var online = _store
            .Where(kv => kv.Value.IsOnline)
            .Select(kv => kv.Key)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<string>>(online);
    }
}
