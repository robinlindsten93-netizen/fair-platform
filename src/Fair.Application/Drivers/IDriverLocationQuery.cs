namespace Fair.Application.Drivers;

public interface IDriverLocationQuery
{
    // Behåll den här om du använder den på andra ställen
    Task<IReadOnlyList<DriverLocationDto>> ListRecentAsync(
        TimeSpan maxAge,
        CancellationToken ct);

    // NY: geo-query för dispatch
    Task<IReadOnlyList<DriverLocationDto>> ListNearbyRecentAsync(
        double centerLat,
        double centerLng,
        int radiusMeters,
        TimeSpan maxAge,
        CancellationToken ct);
}