using Fair.Application.Trips.Queries;
using System.Security.Claims;

namespace Fair.Application.Trips.Queries.Active;

public sealed class GetMyTrips
{
    private readonly ITripReadRepository _read;

    public GetMyTrips(ITripReadRepository read) => _read = read;

    public Task<IReadOnlyList<TripReadDto>> Handle(ClaimsPrincipal user, CancellationToken ct)
    {
        var sub =
            user.FindFirst("sub")?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            throw new InvalidOperationException("Missing user id claim (sub/nameidentifier).");

        if (!Guid.TryParse(sub, out var userId) || userId == Guid.Empty)
            throw new InvalidOperationException("Invalid user id claim (expected Guid).");

        return _read.GetByRiderAsync(userId, ct);
    }
}