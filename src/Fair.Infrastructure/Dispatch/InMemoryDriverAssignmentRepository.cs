using Fair.Application.Dispatch;
using System.Collections.Concurrent;

namespace Fair.Infrastructure.Dispatch;

public sealed class InMemoryDriverAssignmentRepository : IDriverAssignmentRepository
{
    // driverId -> tripId
    private readonly ConcurrentDictionary<Guid, Guid> _assignments = new();

    // driverId -> last time driver became free
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _lastFree = new();

    public Task<DriverAssignResult> TryAssignAsync(Guid driverId, Guid tripId, CancellationToken ct)
    {
        if (_assignments.TryGetValue(driverId, out var existingTripId))
        {
            if (existingTripId == tripId)
                return Task.FromResult(DriverAssignResult.AlreadyAssignedSameTrip);

            return Task.FromResult(DriverAssignResult.AlreadyAssignedOtherTrip);
        }

        var added = _assignments.TryAdd(driverId, tripId);
        if (!added)
            return Task.FromResult(DriverAssignResult.AlreadyAssignedOtherTrip);

        return Task.FromResult(DriverAssignResult.Assigned);
    }

    public Task ReleaseAsync(Guid driverId, Guid tripId, CancellationToken ct)
    {
        if (_assignments.TryGetValue(driverId, out var existingTripId) && existingTripId == tripId)
        {
            _assignments.TryRemove(driverId, out _);

            // mark driver as free now
            _lastFree[driverId] = DateTimeOffset.UtcNow;
        }

        return Task.CompletedTask;
    }

    public Task<bool> IsBusyAsync(Guid driverId, CancellationToken ct)
    {
        return Task.FromResult(_assignments.ContainsKey(driverId));
    }

    // NEW
    public Task<DateTimeOffset?> GetLastFreeAtAsync(Guid driverId, CancellationToken ct)
    {
        if (_lastFree.TryGetValue(driverId, out var ts))
            return Task.FromResult<DateTimeOffset?>(ts);

        return Task.FromResult<DateTimeOffset?>(null);
    }
}