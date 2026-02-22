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
}
