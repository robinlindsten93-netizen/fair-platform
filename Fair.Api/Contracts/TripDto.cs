using Fair.Domain.Trips;

namespace Fair.Api.Contracts;

public sealed record TripDto(
    Guid Id,
    Guid RiderId,
    TransportMode Mode,
    string Status,
    Location Pickup,
    Location Dropoff,
    TripQuote? Quote,
    Guid? DriverId,
    string? VehicleId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc
)
{
    public static TripDto FromDomain(Trip trip)
        => new(
            trip.Id,
            trip.RiderId,
            trip.Mode,
            trip.Status.ToString(), // 🔥 HÄR är magin
            trip.Pickup,
            trip.Dropoff,
            trip.Quote,
            trip.DriverId,
            trip.VehicleId,
            trip.CreatedAtUtc,
            trip.UpdatedAtUtc
        );
}
