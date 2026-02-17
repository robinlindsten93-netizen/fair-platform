using Fair.Domain.Trips;

namespace Fair.Application.Trips.RequestTrip;

public sealed record RequestTripResult(
    Guid TripId,
    TripStatus Status);
