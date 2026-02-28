namespace Fair.Application.Trips.Queries;

public interface ITripReadRepository
{
    Task<TripReadDto?> GetByIdAsync(Guid tripId, CancellationToken ct);

    // Rider “inbox”
    Task<IReadOnlyList<TripReadDto>> GetByRiderAsync(Guid riderId, CancellationToken ct);

    // Driver “inbox” (valfritt men bra)
    Task<IReadOnlyList<TripReadDto>> GetByDriverAsync(Guid driverId, CancellationToken ct);
}