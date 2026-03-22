namespace Fair.Api.Contracts.Driver;

public sealed record UpdateDriverLocationRequest(
    double Lat,
    double Lng
);