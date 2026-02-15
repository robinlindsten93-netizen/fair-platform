using Fair.Domain.Trips;

namespace Fair.Application.Trips;

public interface ITripRepository
{
    Task<Trip?> GetByIdAsync(Guid tripId, CancellationToken ct = default);

    Task AddAsync(Trip trip, CancellationToken ct = default);

    Task UpdateAsync(Trip trip, CancellationToken ct = default);
}
