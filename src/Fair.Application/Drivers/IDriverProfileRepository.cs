namespace Fair.Application.Drivers;

public interface IDriverProfileRepository
{
    Task<DriverMeDto> GetAsync(string userId, CancellationToken ct);
    Task<DriverMeDto> SetAvailabilityAsync(string userId, bool isOnline, CancellationToken ct);
}
