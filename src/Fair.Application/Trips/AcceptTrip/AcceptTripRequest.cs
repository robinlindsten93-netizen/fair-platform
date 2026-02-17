namespace Fair.Application.Trips.AcceptTrip;

public sealed record AcceptTripRequest(
    Guid TripId,
    Guid DriverId,
    string VehicleId
);
