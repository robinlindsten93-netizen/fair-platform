using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Fair.Api.Hubs;

public sealed class DispatchHub : Hub
{
    private readonly ILogger<DispatchHub> _log;

    public DispatchHub(ILogger<DispatchHub> log)
    {
        _log = log;
    }

    public override Task OnConnectedAsync()
    {
        _log.LogInformation("DispatchHub connected connectionId={ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _log.LogInformation("DispatchHub disconnected connectionId={ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public Task JoinDriverGroup(string driverId)
    {
        _log.LogInformation("DispatchHub join driver group connectionId={ConnectionId} driverId={DriverId}",
            Context.ConnectionId, driverId);

        return Groups.AddToGroupAsync(Context.ConnectionId, $"driver:{driverId}");
    }

    public Task JoinRiderGroup(string riderId)
    {
        _log.LogInformation("DispatchHub join rider group connectionId={ConnectionId} riderId={RiderId}",
            Context.ConnectionId, riderId);

        return Groups.AddToGroupAsync(Context.ConnectionId, $"rider:{riderId}");
    }
}