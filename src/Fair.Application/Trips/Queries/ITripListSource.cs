using Fair.Domain.Trips;

namespace Fair.Application.Trips.Queries;

// InMemory/Dev convenience. DB-implementation kan ignorera detta.
public interface ITripListSource
{
    Task<IReadOnlyList<Trip>> ListAllAsync(CancellationToken ct);
}