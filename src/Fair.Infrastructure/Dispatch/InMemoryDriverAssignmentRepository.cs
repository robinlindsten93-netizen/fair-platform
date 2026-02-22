using Fair.Application.Dispatch;
using System.Collections.Concurrent;

namespace Fair.Infrastructure.Dispatch;

public sealed class InMemoryDriverAssignmentRepository : IDriverAssignmentRepository
{
    // driverId -> tripId
    private readonly ConcurrentDictionary<Guid, Guid> _active = new();

    public Task<DriverAssignResult> TryAssignAsync(Guid driverId, Guid tripId, CancellationToken ct)
    {
        if (_active.TryGetValue(driverId, out var existing))
        {
            if (existing == tripId)
                return Task.FromResult(DriverAssignResult.AlreadyAssignedSameTrip);

            return Task.FromResult(DriverAssignResult.AlreadyAssignedOtherTrip);
        }

        var ok = _active.TryAdd(driverId, tripId);
        return Task.FromResult(ok ? DriverAssignResult.Assigned : DriverAssignResult.AlreadyAssignedOtherTrip);
    }

    public Task ReleaseAsync(Guid driverId, Guid tripId, CancellationToken ct)
    {
        if (_active.TryGetValue(driverId, out var existing) && existing == tripId)
            _active.TryRemove(driverId, out _);

        return Task.CompletedTask;
    }
}

