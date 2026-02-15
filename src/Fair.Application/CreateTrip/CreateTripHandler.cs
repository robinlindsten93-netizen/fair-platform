using Fair.Domain.Trips;

namespace Fair.Application.Trips.CreateTrip;

public sealed class CreateTripHandler
{
    private readonly ITripRepository _repo;

    public CreateTripHandler(ITripRepository repo) => _repo = repo;

    public async Task<CreateTripResult> HandleAsync(CreateTripRequest req, CancellationToken ct = default)
    {
        var pickup = Location.Create(req.PickupLat, req.PickupLng);
        var dropoff = Location.Create(req.DropoffLat, req.DropoffLng);

        var trip = Trip.CreateDraft(req.RiderId, pickup, dropoff, req.Mode);

        await _repo.AddAsync(trip, ct);
        return new CreateTripResult(trip.Id);
    }
}
