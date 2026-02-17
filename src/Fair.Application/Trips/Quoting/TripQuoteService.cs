using Fair.Application.Trips.Quoting;
using Fair.Domain.Trips;

namespace Fair.Infrastructure.Trips.Quoting;

public sealed class TripQuoteService : ITripQuoteService
{
    public TripQuote CreateQuote(
        Location pickup,
        Location dropoff,
        TransportMode mode,
        DateTimeOffset nowUtc)
    {
        var distanceMeters = EstimateDistanceMeters(pickup, dropoff);

        var speedMetersPerSecond = 35_000.0 / 3600.0; // ~35 km/h
        var durationSeconds =
            (int)Math.Ceiling(distanceMeters / speedMetersPerSecond) + 180;

        var baseFee = 45m;
        var perKm = 14m;
        var perMin = 3.5m;

        var km = distanceMeters / 1000m;
        var minutes = durationSeconds / 60m;

        var amount = baseFee + (perKm * km) + (perMin * minutes);
        amount = Math.Round(amount, 0, MidpointRounding.AwayFromZero);

        var price = Money.Create(amount, "SEK");

        // 🔑 TTL styrs här (5 min nu)
        var expiresAt = nowUtc.AddMinutes(5);

        return TripQuote.Create(
            distanceMeters,
            durationSeconds,
            price,
            expiresAt,
            surgeMultiplier: null);
    }

    private static int EstimateDistanceMeters(Location a, Location b)
    {
        const double R = 6371000;

        double dLat = DegreesToRadians(b.Latitude - a.Latitude);
        double dLon = DegreesToRadians(b.Longitude - a.Longitude);

        double lat1 = DegreesToRadians(a.Latitude);
        double lat2 = DegreesToRadians(b.Latitude);

        double h =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1) * Math.Cos(lat2) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(h), Math.Sqrt(1 - h));
        return (int)Math.Round(R * c);
    }

    private static double DegreesToRadians(double deg)
        => deg * (Math.PI / 180.0);
}

