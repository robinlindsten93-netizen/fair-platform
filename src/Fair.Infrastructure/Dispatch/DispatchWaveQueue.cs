using System.Collections.Generic;

namespace Fair.Infrastructure.Dispatch;

public sealed class DispatchWaveQueue
{
    public sealed record Job(Guid TripId, int TripVersion, DateTimeOffset DueAtUtc);

    private readonly object _gate = new();

    // Prioritetskö (min-heap) på DueAt
    private readonly PriorityQueue<Job, long> _pq = new();

    // Signal för att väcka DequeueAsync när något schemaläggs
    private readonly SemaphoreSlim _signal = new(0, int.MaxValue);

    // tick counter för att kunna “bryta” väntan när ny tidigare job kommer in
    private long _scheduleTick = 0;

    public void Schedule(Guid tripId, int tripVersion, DateTimeOffset dueAtUtc)
    {
        lock (_gate)
        {
            var job = new Job(tripId, tripVersion, dueAtUtc);
            var prio = dueAtUtc.UtcTicks;

            _pq.Enqueue(job, prio);
            _scheduleTick++;
        }

        // väck en väntande dequeue
        _signal.Release();
    }

    /// <summary>
    /// Blockerar tills nästa job är "due" och returnerar det.
    /// </summary>
    public async Task<Job> DequeueAsync(CancellationToken ct)
    {
        while (true)
        {
            Job? next;
            long tickSnapshot;
            TimeSpan delay;

            lock (_gate)
            {
                tickSnapshot = _scheduleTick;

                if (_pq.Count == 0)
                {
                    next = null;
                    delay = Timeout.InfiniteTimeSpan;
                }
                else
                {
                    next = _pq.Peek();
                    var now = DateTimeOffset.UtcNow;
                    delay = next.DueAtUtc <= now ? TimeSpan.Zero : (next.DueAtUtc - now);
                }
            }

            // Inget i kön: vänta tills någon schemalägger
            if (next is null)
            {
                await _signal.WaitAsync(ct);
                continue;
            }

            // Om due nu: plocka och returnera
            if (delay == TimeSpan.Zero)
            {
                lock (_gate)
                {
                    // Re-check under lock (för säkerhet)
                    if (_pq.Count == 0) continue;

                    var peek = _pq.Peek();
                    if (peek.DueAtUtc > DateTimeOffset.UtcNow)
                        continue;

                    return _pq.Dequeue();
                }
            }

            // Vänta tills job blir due, men avbryt om ny schedule sker (ev. tidigare job)
            var waitTask = Task.Delay(delay, ct);
            var signalTask = _signal.WaitAsync(ct);

            var completed = await Task.WhenAny(waitTask, signalTask);

            // Om vi vaknade av signal, loopa och räkna om (kanske kom tidigare job)
            if (completed == signalTask)
            {
                // konsumera signal (vi bryr oss inte om vilken)
                // och loopa för att re-evaluera delay/peek
                continue;
            }

            // annars blev waitTask klar -> loop och plocka
            // (vi loopar för att plocka under lock)
        }
    }
}