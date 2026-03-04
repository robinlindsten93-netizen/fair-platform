using Fair.Application.Drivers;
using System.Collections.Concurrent;

namespace Fair.Infrastructure.Drivers;

// SINGLETON (registreras som både Query + Writer)
public sealed class InMemoryDriverLocationRepository : IDriverLocationWriter, IDriverLocationQuery
{
    // Senaste position per driver
    private readonly ConcurrentDictionary<Guid, DriverLocationDto> _latest = new();

    // Enkel grid-index: nyckel = cell (latBucket, lngBucket) -> driverIds
    // BucketSizeDegrees ≈ 0.01° ~ 1.1km i lat-led. Bra “dev-index”.
    private const double BucketSizeDegrees = 0.01;

    private readonly object _gate = new();
    private readonly Dictionary<(int LatB, int LngB), HashSet<Guid>> _cells = new();

    public Task UpsertAsync(DriverLocationDto location, CancellationToken ct)
    {
        _latest.AddOrUpdate(location.DriverId, location, (_, __) => location);

        lock (_gate)
        {
            // Ta bort driver från tidigare cell (om fanns)
            // (Vi gör en enkel scan här; i prod hade vi haft reverse-index driver->cell)
            foreach (var kv in _cells)
                kv.Value.Remove(location.DriverId);

            var key = CellKey(location.Lat, location.Lng);
            if (!_cells.TryGetValue(key, out var set))
            {
                set = new HashSet<Guid>();
                _cells[key] = set;
            }
            set.Add(location.DriverId);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<DriverLocationDto>> ListRecentAsync(TimeSpan maxAge, CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow - maxAge;

        var res = _latest.Values
            .Where(x => x.RecordedAtUtc >= cutoff)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<DriverLocationDto>>(res);
    }

    public Task<IReadOnlyList<DriverLocationDto>> ListNearbyRecentAsync(
        double centerLat,
        double centerLng,
        int radiusMeters,
        TimeSpan maxAge,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var cutoff = now - maxAge;

        // Bounding box (snabb grovfilter)
        var (minLat, maxLat, minLng, maxLng) = BoundingBox(centerLat, centerLng, radiusMeters);

        var keys = CellsForBox(minLat, maxLat, minLng, maxLng);

        HashSet<Guid> candidateIds = new();
        lock (_gate)
        {
            foreach (var k in keys)
            {
                if (_cells.TryGetValue(k, out var set))
                {
                    foreach (var id in set)
                        candidateIds.Add(id);
                }
            }
        }

        // Exakt filter: TTL + radie via Haversine
        var result = new List<DriverLocationDto>(candidateIds.Count);
        foreach (var id in candidateIds)
        {
            if (!_latest.TryGetValue(id, out var loc))
                continue;

            if (loc.RecordedAtUtc < cutoff)
                continue;

            var d = HaversineMeters(centerLat, centerLng, loc.Lat, loc.Lng);
            if (d <= radiusMeters)
                result.Add(loc);
        }

        return Task.FromResult<IReadOnlyList<DriverLocationDto>>(result.AsReadOnly());
    }

    private static (int LatB, int LngB) CellKey(double lat, double lng)
        => ((int)Math.Floor(lat / BucketSizeDegrees), (int)Math.Floor(lng / BucketSizeDegrees));

    private static IEnumerable<(int LatB, int LngB)> CellsForBox(double minLat, double maxLat, double minLng, double maxLng)
    {
        var minLatB = (int)Math.Floor(minLat / BucketSizeDegrees);
        var maxLatB = (int)Math.Floor(maxLat / BucketSizeDegrees);
        var minLngB = (int)Math.Floor(minLng / BucketSizeDegrees);
        var maxLngB = (int)Math.Floor(maxLng / BucketSizeDegrees);

        for (var latB = minLatB; latB <= maxLatB; latB++)
            for (var lngB = minLngB; lngB <= maxLngB; lngB++)
                yield return (latB, lngB);
    }

    private static (double minLat, double maxLat, double minLng, double maxLng) BoundingBox(double lat, double lng, int radiusMeters)
    {
        // 1 deg lat ≈ 111_320 m
        var dLat = radiusMeters / 111_320d;

        // 1 deg lng ≈ 111_320 * cos(lat)
        var cos = Math.Cos(lat * Math.PI / 180d);
        var dLng = radiusMeters / (111_320d * Math.Max(0.000001, cos));

        return (lat - dLat, lat + dLat, lng - dLng, lng + dLng);
    }

    private static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6_371_000; // meter
        var dLat = (lat2 - lat1) * Math.PI / 180d;
        var dLng = (lng2 - lng1) * Math.PI / 180d;

        var a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180d) * Math.Cos(lat2 * Math.PI / 180d) *
            Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}