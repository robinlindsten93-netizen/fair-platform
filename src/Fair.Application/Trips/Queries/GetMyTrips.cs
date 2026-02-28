using System.Security.Claims;

namespace Fair.Application.Trips.Queries;

public sealed class GetMyTrips
{
    private readonly ITripReadRepository _read;

    public GetMyTrips(ITripReadRepository read) => _read = read;

    public Task<IReadOnlyList<TripReadDto>> HandleAsRider(ClaimsPrincipal user, CancellationToken ct)
    {
        var riderId = GetUserIdOrThrow(user);
        return _read.GetByRiderAsync(riderId, ct);
    }

    public Task<IReadOnlyList<TripReadDto>> HandleAsDriver(ClaimsPrincipal user, CancellationToken ct)
    {
        var driverId = GetUserIdOrThrow(user);
        return _read.GetByDriverAsync(driverId, ct);
    }

    private static Guid GetUserIdOrThrow(ClaimsPrincipal user)
    {
        var sub =
            user.FindFirst("sub")?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(sub, out var id) && id != Guid.Empty) return id;
        throw new InvalidOperationException("missing_or_invalid_sub");
    }
}