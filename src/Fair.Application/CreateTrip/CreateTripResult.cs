using Fair.Domain.Trips;

namespace Fair.Application.Trips.CreateTrip;

public sealed record CreateTripResult(Guid TripId, TripStatus Status);