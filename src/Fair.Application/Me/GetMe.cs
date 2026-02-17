using Fair.Application.Auth;
using Fair.Domain.Auth;
using System.Security.Claims;

namespace Fair.Application.Me;

public sealed class GetMe
{
    private readonly IRoleAssignmentRepository _roles;

    public GetMe(IRoleAssignmentRepository roles)
    {
        _roles = roles;
    }

    public async Task<MeDto> Handle(ClaimsPrincipal user, CancellationToken ct)
    {
        // Undvik FindFirstValue (extension) och läs claims robust
        var userId =
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            user.FindFirst("sub")?.Value ??
            throw new InvalidOperationException("Missing user id claim (sub/nameidentifier).");

        var phone = user.FindFirst("phone")?.Value;

        // Gör list explicit så Count alltid är property (inte method group)
        var assignments = (await _roles.GetActiveByUserId(userId, ct)).ToList();

        var roles = assignments.Count == 0
            ? new[] { Role.Rider }
            : assignments.Select(a => a.Role).Distinct().ToArray();

        var fleetScopes = assignments
            .Where(a => a.FleetId is not null)
            .Select(a => a.FleetId!)
            .Distinct()
            .ToArray();

        return new MeDto(userId, phone, roles, fleetScopes);
    }
}

