using Fair.Api.Hubs;
using Fair.Application.Dispatch;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Fair.Api.Realtime;

public sealed class SignalRDispatchNotifier : IDispatchNotifier
{
    private readonly IHubContext<DispatchHub> _hub;
    private readonly ILogger<SignalRDispatchNotifier> _log;

    public SignalRDispatchNotifier(
        IHubContext<DispatchHub> hub,
        ILogger<SignalRDispatchNotifier> log)
    {
        _hub = hub;
        _log = log;
    }

    public Task NotifyDriverNewOffer(Guid driverId, object payload, CancellationToken ct)
    {
        _log.LogInformation("SignalR notify driver group driver:{DriverId}", driverId);

        return _hub.Clients
            .Group($"driver:{driverId}")
            .SendAsync("new-offer", payload, ct);
    }
}