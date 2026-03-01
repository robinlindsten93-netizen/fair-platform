namespace Fair.Application.Dispatch;

public interface IDriverAvailabilityQuery
{
    // Bulk lookup (dispatch fan-out)
    Task<IReadOnlyList<Guid>> GetOnlineDriverIdsAsync(CancellationToken ct);

    // Point lookup (guards / fast checks)
    Task<bool> IsDriverOnlineAsync(Guid driverId, CancellationToken ct);
}