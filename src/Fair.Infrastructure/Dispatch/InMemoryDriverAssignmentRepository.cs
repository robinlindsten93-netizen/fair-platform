using Fair.Application.Dispatch;
using System.Collections.Concurrent;

namespace Fair.Infrastructure.Dispatch;

public sealed class InMemoryDriverAssignmentRepository : IDriverAssignmentRepository
{
    // driverId -> tripId
    private readonly ConcurrentDictionary<Guid, Guid> _byDriver = new();

    public Task<DriverAssignResult> TryAssignAsync(Guid driverId, Guid tripId, CancellationToken ct)
    {
        // Om driver redan assigned till samma trip -> idempotent ok
        if (_byDriver.TryGetValue(driverId, out var existing) && existing == tripId)
            return Task.FromResult(DriverAssignResult.Assigned);

        // Om driver assigned till annan trip -> fail
        if (_byDriver.TryGetValue(driverId, out existing) && existing != tripId)
            return Task.FromResult(DriverAssignResult.AlreadyAssignedOtherTrip);

        // Försök assign
        var ok = _byDriver.TryAdd(driverId, tripId);
        return Task.FromResult(ok ? DriverAssignResult.Assigned : DriverAssignResult.AlreadyAssignedOtherTrip);
    }

    public Task ReleaseAsync(Guid driverId, Guid tripId, CancellationToken ct)
    {
        // släpp bara om den pekar på tripId (säker release)
        if (_byDriver.TryGetValue(driverId, out var existing) && existing == tripId)
            _byDriver.TryRemove(driverId, out _);

        return Task.CompletedTask;
    }

    public Task<Guid?> GetAssignedTripIdAsync(Guid driverId, CancellationToken ct)
    {
        return Task.FromResult(_byDriver.TryGetValue(driverId, out var tripId) ? (Guid?)tripId : null);
    }
}