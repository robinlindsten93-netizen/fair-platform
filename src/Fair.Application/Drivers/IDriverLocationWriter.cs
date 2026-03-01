namespace Fair.Application.Drivers;

public interface IDriverLocationWriter
{
    Task UpsertAsync(DriverLocationDto location, CancellationToken ct);
}