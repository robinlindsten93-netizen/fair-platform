using Fair.Api.Hubs;
using Fair.Application.Trips;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Fair.Api.Realtime;

public sealed class SignalRTripNotifier : ITripNotifier
{
    private readonly IHubContext<DispatchHub> _hub;
    private readonly ILogger<SignalRTripNotifier> _log;

    public SignalRTripNotifier(
        IHubContext<DispatchHub> hub,
        ILogger<SignalRTripNotifier> log)
    {
        _hub = hub;
        _log = log;
    }

    public Task NotifyRiderTripStatusChanged(Guid riderId, object payload, CancellationToken ct)
    {
        _log.LogInformation("SignalR notify rider trip status rider:{RiderId}", riderId);

        return _hub.Clients
            .Group($"rider:{riderId}")
            .SendAsync("trip-status-changed", payload, ct);
    }

    public Task NotifyDriverTripStatusChanged(Guid driverId, object payload, CancellationToken ct)
    {
        _log.LogInformation("SignalR notify driver trip status driver:{DriverId}", driverId);

        return _hub.Clients
            .Group($"driver:{driverId}")
            .SendAsync("trip-status-changed", payload, ct);
    }
}