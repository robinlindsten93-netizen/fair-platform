using Fair.Domain.Trips;

namespace Fair.Application.Trips.CompleteTrip;

public sealed record CompleteTripResult(Guid TripId, TripStatus Status);
