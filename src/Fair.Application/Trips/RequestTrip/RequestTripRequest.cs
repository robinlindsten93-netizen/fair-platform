namespace Fair.Application.Trips.RequestTrip;

public sealed record RequestTripRequest(
    Guid TripId,
    string QuoteToken
);