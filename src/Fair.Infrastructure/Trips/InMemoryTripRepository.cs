using Fair.Application.Trips;
using Fair.Domain.Trips;
using System.Collections.Concurrent;

namespace Fair.Infrastructure.Trips;

public sealed class InMemoryTripRepository : ITripRepository
{
    private readonly ConcurrentDictionary<Guid, Trip> _store = new();

    public Task AddAsync(Trip trip, CancellationToken ct = default)
    {
        if (!_store.TryAdd(trip.Id, trip))
            throw new InvalidOperationException($"Trip already exists: {trip.Id}");

        return Task.CompletedTask;
    }

    public Task<Trip?> GetByIdAsync(Guid tripId, CancellationToken ct = default)
    {
        _store.TryGetValue(tripId, out var trip);
        return Task.FromResult(trip);
    }

    public Task UpdateAsync(Trip trip, CancellationToken ct = default)
    {
        // In-memory: trip är referens-objekt så detta är i praktiken en no-op.
        // Men vi behåller metoden för att matcha framtida DB-implementation.
        _store[trip.Id] = trip;
        return Task.CompletedTask;
    }
}
