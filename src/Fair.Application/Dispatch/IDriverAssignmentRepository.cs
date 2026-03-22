namespace Fair.Application.Dispatch;

public enum DriverAssignResult
{
    Assigned,
    AlreadyAssignedSameTrip,
    AlreadyAssignedOtherTrip
}

public interface IDriverAssignmentRepository
{
    Task<DriverAssignResult> TryAssignAsync(Guid driverId, Guid tripId, CancellationToken ct);

    Task ReleaseAsync(Guid driverId, Guid tripId, CancellationToken ct);

    // används i dispatch för att filtrera bort upptagna drivers
    Task<bool> IsBusyAsync(Guid driverId, CancellationToken ct);

    // NEW: fairness (idle time)
    Task<DateTimeOffset?> GetLastFreeAtAsync(Guid driverId, CancellationToken ct);
}