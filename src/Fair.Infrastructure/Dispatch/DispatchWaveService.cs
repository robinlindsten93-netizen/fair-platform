using Fair.Application.Dispatch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fair.Infrastructure.Dispatch;

public sealed class DispatchWaveService : BackgroundService
{
    private readonly DispatchWaveQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DispatchWaveService> _log;

    public DispatchWaveService(
        DispatchWaveQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<DispatchWaveService> log)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _log.LogInformation("DispatchWaveService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            DispatchWaveQueue.Job job;

            try
            {
                job = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var uc = scope.ServiceProvider.GetRequiredService<CreateDispatchOffers>();

                await uc.Handle(job.TripId, job.TripVersion, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogError(
                    ex,
                    "DispatchWaveService job failed. tripId={TripId} version={TripVersion}",
                    job.TripId,
                    job.TripVersion);
            }
        }

        _log.LogInformation("DispatchWaveService stopped.");
    }
}