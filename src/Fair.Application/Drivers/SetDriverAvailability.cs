using System.Security.Claims;

namespace Fair.Application.Drivers;

public sealed class SetDriverAvailability
{
    private readonly IDriverProfileRepository _repo;

    public SetDriverAvailability(IDriverProfileRepository repo) => _repo = repo;

    public Task<DriverMeDto> Handle(ClaimsPrincipal user, bool isOnline, CancellationToken ct)
    {
        var userId =
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            user.FindFirst("sub")?.Value ??
            throw new InvalidOperationException("Missing user id claim (sub/nameidentifier).");

        return _repo.SetAvailabilityAsync(userId, isOnline, ct);
    }
}
