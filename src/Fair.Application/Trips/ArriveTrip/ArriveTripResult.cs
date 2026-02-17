using Fair.Domain.Trips;

namespace Fair.Application.Trips.ArriveTrip;

public sealed record ArriveTripResult(Guid TripId, TripStatus Status);
