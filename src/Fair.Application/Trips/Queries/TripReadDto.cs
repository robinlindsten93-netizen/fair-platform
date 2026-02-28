namespace Fair.Application.Trips.Queries;

public sealed record TripReadDto(
    Guid Id,
    Guid RiderId,
    int Mode,
    string Status,
    double PickupLat,
    double PickupLng,
    double DropoffLat,
    double DropoffLng,
    TripQuoteReadDto? Quote,
    Guid? DriverId,
    string? VehicleId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version
);

public sealed record TripQuoteReadDto(
    int EstimatedDistanceMeters,
    int EstimatedDurationSeconds,
    MoneyReadDto Price,
    DateTimeOffset ExpiresAtUtc,
    decimal? SurgeMultiplier
);

public sealed record MoneyReadDto(
    decimal Amount,
    string Currency
);