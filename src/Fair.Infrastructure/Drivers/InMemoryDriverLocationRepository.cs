using Fair.Application.Drivers;
using System.Collections.Concurrent;

namespace Fair.Infrastructure.Drivers;

public sealed class InMemoryDriverLocationRepository :
    IDriverLocationWriter,
    IDriverLocationQuery
{
    // Senaste position per driver
    private readonly ConcurrentDictionary<Guid, DriverLocationDto> _latest = new();

    public Task UpsertAsync(DriverLocationDto location, CancellationToken ct)
    {
        _latest.AddOrUpdate(
            location.DriverId,
            location,
            (_, existing) =>
            {
                // Behåll newest (skydd mot out-of-order updates)
                if (location.ObservedAtUtc >= existing.ObservedAtUtc) return location;
                return existing;
            });

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DriverLocationDto>> ListRecentAsync(TimeSpan maxAge, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now - maxAge;

        var snapshot = _latest.Values
            .Where(x => x.ObservedAtUtc >= cutoff)
            .OrderByDescending(x => x.ObservedAtUtc)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<DriverLocationDto>>(snapshot);
    }
}