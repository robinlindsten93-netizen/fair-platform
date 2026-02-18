using System.Security.Claims;

namespace Fair.Application.Drivers;

public sealed class GetDriverMe
{
    private readonly IDriverProfileRepository _repo;

    public GetDriverMe(IDriverProfileRepository repo) => _repo = repo;

    public Task<DriverMeDto> Handle(ClaimsPrincipal user, CancellationToken ct)
    {
        var userId =
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            user.FindFirst("sub")?.Value ??
            throw new InvalidOperationException("Missing user id claim (sub/nameidentifier).");

        return _repo.GetAsync(userId, ct);
    }
}
