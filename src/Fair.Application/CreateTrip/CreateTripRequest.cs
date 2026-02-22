namespace Fair.Application.Trips.CreateTrip;

public sealed record CreateTripRequest(
    Guid RiderId,
    double PickupLat,
    double PickupLng,
    double DropoffLat,
    double DropoffLng,
    int Mode,
    string QuoteToken
);