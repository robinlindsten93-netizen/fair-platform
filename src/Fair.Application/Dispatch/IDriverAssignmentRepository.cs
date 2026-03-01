namespace Fair.Application.Dispatch;

public enum DriverAssignResult
{
    Assigned,
    AlreadyAssignedSameTrip,
    AlreadyAssignedOtherTrip
}

public interface IDriverAssignmentRepository
{
    /// <summary>
    /// Try to assign driver to trip.
    /// Must be atomic.
    /// </summary>
    Task<DriverAssignResult> TryAssignAsync(
        Guid driverId,
        Guid tripId,
        CancellationToken ct);

    /// <summary>
    /// Release driver from trip (best-effort).
    /// </summary>
    Task ReleaseAsync(
        Guid driverId,
        Guid tripId,
        CancellationToken ct);

    // =========================
    // 🔥 NEW — required for guards & read models
    // =========================
    /// <summary>
    /// Returns the currently assigned trip for driver, if any.
    /// Used by guards and active trip queries.
    /// </summary>
    Task<Guid?> GetAssignedTripIdAsync(
        Guid driverId,
        CancellationToken ct);
}