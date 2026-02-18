namespace Fair.Application.Dispatch;

public interface IDriverAvailabilityQuery
{
    Task<IReadOnlyList<string>> GetOnlineDriverIdsAsync(CancellationToken ct);
}
