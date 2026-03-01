namespace Fair.Application.Drivers;

public interface IDriverLocationQuery
{
    Task<IReadOnlyList<DriverLocationDto>> ListRecentAsync(
        TimeSpan maxAge,
        CancellationToken ct);
}