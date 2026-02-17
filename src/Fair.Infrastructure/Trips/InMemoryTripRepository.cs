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
        // bump version
        trip.IncrementVersion();

        _store[trip.Id] = trip;
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(Trip trip, int expectedVersion, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(trip.Id, out var existing))
            return Task.FromResult(false);

        if (existing.Version != expectedVersion)
            return Task.FromResult(false);

        // bump version
        trip.IncrementVersion();

        _store[trip.Id] = trip;
        return Task.FromResult(true);
    }
}

