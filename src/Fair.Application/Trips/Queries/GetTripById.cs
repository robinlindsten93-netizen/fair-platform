namespace Fair.Application.Trips.Queries;

public sealed class GetTripById
{
    private readonly ITripReadRepository _read;

    public GetTripById(ITripReadRepository read) => _read = read;

    public Task<TripReadDto?> Handle(Guid tripId, CancellationToken ct)
        => _read.GetByIdAsync(tripId, ct);
}