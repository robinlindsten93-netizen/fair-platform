using Fair.Application.Trips;
using Fair.Domain.Trips;
using System.Collections.Concurrent;

namespace Fair.Infrastructure.Trips;

public sealed class InMemoryTripRepository : ITripRepository
{
    private readonly ConcurrentDictionary<Guid, Trip> _store = new();

    // =========================
    // ADD
    // =========================
    public Task AddAsync(Trip trip, CancellationToken ct = default)
    {
        if (trip is null)
            throw new ArgumentNullException(nameof(trip));

        if (!_store.TryAdd(trip.Id, trip))
            throw new InvalidOperationException($"Trip already exists: {trip.Id}");

        return Task.CompletedTask;
    }

    // =========================
    // GET BY ID
    // =========================
    public Task<Trip?> GetByIdAsync(Guid tripId, CancellationToken ct = default)
    {
        _store.TryGetValue(tripId, out var trip);
        return Task.FromResult(trip);
    }

    // =========================
    // FORCE UPDATE (legacy/backoffice)
    // =========================
    public Task UpdateAsync(Trip trip, CancellationToken ct = default)
    {
        if (trip is null)
            throw new ArgumentNullException(nameof(trip));

        // ⚠️ Använd sparsamt — bypassar optimistic concurrency
        trip.IncrementVersion();
        _store[trip.Id] = trip;

        return Task.CompletedTask;
    }

    // =========================
    // OPTIMISTIC CONCURRENCY UPDATE
    // =========================
    public Task<bool> UpdateAsync(Trip trip, int expectedVersion, CancellationToken ct = default)
    {
        if (trip is null)
            throw new ArgumentNullException(nameof(trip));

        while (true)
        {
            if (!_store.TryGetValue(trip.Id, out var existing))
                return Task.FromResult(false);

            if (existing.Version != expectedVersion)
                return Task.FromResult(false);

            // bump version på inkommande modellen
            trip.IncrementVersion();

            // CAS-liknande beteende
            if (_store.TryUpdate(trip.Id, trip, existing))
                return Task.FromResult(true);

            // någon hann emellan → retry
        }
    }
}
