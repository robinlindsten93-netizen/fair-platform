using Fair.Domain.Trips;

namespace Fair.Application.Trips.AcceptTrip;

public sealed record AcceptTripResult(
    Guid TripId,
    TripStatus Status
);
