namespace Fair.Application.Trips;

public interface ITripNotifier
{
    Task NotifyRiderTripStatusChanged(Guid riderId, object payload, CancellationToken ct);
    Task NotifyDriverTripStatusChanged(Guid driverId, object payload, CancellationToken ct);
}