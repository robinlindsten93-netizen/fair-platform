using Fair.Application.Dispatch;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fair.Infrastructure.Dispatch;

public sealed class DispatchOfferExpiryService : BackgroundService
{
    private readonly IDispatchOfferRepository _offers;
    private readonly ILogger<DispatchOfferExpiryService> _log;
    private readonly DispatchOptions _opts;

    public DispatchOfferExpiryService(
        IDispatchOfferRepository offers,
        IOptions<DispatchOptions> opts,
        ILogger<DispatchOfferExpiryService> log)
    {
        _offers = offers;
        _log = log;
        _opts = opts.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(Math.Max(1, _opts.ExpireSweepSeconds));

        _log.LogInformation("DispatchOfferExpiryService started. sweep={SweepSeconds}s", delay.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                await _offers.ExpireOffersAsync(now, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // normal shutdown
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "DispatchOfferExpiryService sweep failed");
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // normal shutdown
            }
        }

        _log.LogInformation("DispatchOfferExpiryService stopped.");
    }
}