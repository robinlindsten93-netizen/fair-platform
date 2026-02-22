namespace Fair.Application.Dispatch;

public interface IDriverAvailabilityQuery
{
    Task<IReadOnlyList<Guid>> GetOnlineDriverIdsAsync(CancellationToken ct);
}
