namespace Fair.Application.Drivers;

public sealed record DriverLocationDto(
    Guid DriverId,
    double Lat,
    double Lng,
    DateTimeOffset ObservedAtUtc
);