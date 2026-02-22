namespace Fair.Application.Dispatch;

public sealed record DispatchOfferDto(
    Guid OfferId,
    Guid TripId,
    Guid DriverId,
    int TripVersion,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Status
);
