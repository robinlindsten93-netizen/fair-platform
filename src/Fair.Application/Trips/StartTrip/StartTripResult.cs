using Fair.Domain.Trips;

namespace Fair.Application.Trips.StartTrip;

public sealed record StartTripResult(Guid TripId, TripStatus Status);
