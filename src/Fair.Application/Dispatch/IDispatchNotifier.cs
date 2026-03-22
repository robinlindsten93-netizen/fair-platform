namespace Fair.Application.Dispatch;

public interface IDispatchNotifier
{
    Task NotifyDriverNewOffer(Guid driverId, object payload, CancellationToken ct);
}