using System.Collections.Concurrent;

namespace Fair.Infrastructure.Dispatch;

public sealed class DispatchWaveQueue
{
    public sealed record Job(Guid TripId, int TripVersion, DateTimeOffset NextWaveAtUtc);

    private readonly ConcurrentDictionary<(Guid TripId, int TripVersion), Job> _jobs = new();

    public void Schedule(Guid tripId, int tripVersion, DateTimeOffset nextWaveAtUtc)
        => _jobs[(tripId, tripVersion)] = new Job(tripId, tripVersion, nextWaveAtUtc);

    public bool TryTakeDue(DateTimeOffset nowUtc, out Job job)
    {
        foreach (var kv in _jobs)
        {
            if (kv.Value.NextWaveAtUtc <= nowUtc)
            {
                if (_jobs.TryRemove(kv.Key, out job!))
                    return true;
            }
        }

        job = default!;
        return false;
    }
}