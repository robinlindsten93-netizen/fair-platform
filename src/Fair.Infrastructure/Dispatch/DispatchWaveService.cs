using Fair.Application.Dispatch;
using Fair.Application.Drivers;
using Fair.Application.Trips;
using Fair.Domain.Trips;
using Microsoft.Extensions.Hosting;

namespace Fair.Infrastructure.Dispatch;

public sealed class DispatchWaveService : BackgroundService
{
    private readonly DispatchWaveQueue _queue;
    private readonly CreateDispatchOffers _create; // vi återanvänder use case
    private readonly ITripRepository _trips;

    public DispatchWaveService(
        DispatchWaveQueue queue,
        CreateDispatchOffers create,
        ITripRepository trips)
    {
        _queue = queue;
        _create = create;
        _trips = trips;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;

            if (_queue.TryTakeDue(now, out var job))
            {
                // Om trip redan inte är Requested -> inga fler waves
                var trip = await _trips.GetByIdAsync(job.TripId, stoppingToken);
                if (trip is null || trip.Status != TripStatus.Requested)
                    continue;

                // Kör "wave dispatch" igen (use case är idempotent per version, men vi gör waves via offered-driver set)
                await _create.Handle(job.TripId, job.TripVersion, stoppingToken);
            }

            await Task.Delay(250, stoppingToken);
        }
    }
}