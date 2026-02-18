using Fair.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Fair.Api.AuthZ;

public sealed class RoleAuthorizationHandler : IAuthorizationHandler
{
    private readonly IRoleAssignmentRepository _rolesRepo;

    public RoleAuthorizationHandler(IRoleAssignmentRepository rolesRepo)
        => _rolesRepo = rolesRepo;

    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var userId =
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            return;

        var assignments = await _rolesRepo.GetActiveByUserId(userId, CancellationToken.None);

        foreach (var requirement in context.Requirements)
        {
            // App-wide role (FleetId == null)
            if (requirement is RequireAppRoleRequirement appRoleReq)
            {
                var ok = assignments.Any(a =>
                    a.FleetId is null &&
                    string.Equals(a.Role, appRoleReq.Role, StringComparison.OrdinalIgnoreCase));

                if (ok) context.Succeed(requirement);
                continue;
            }

            // Fleet-scoped role (FleetId == route {fleetId})
            if (requirement is RequireFleetRoleRequirement fleetReq)
            {
                var fleetId = GetRouteValue(context, "fleetId");
                if (string.IsNullOrWhiteSpace(fleetId))
                    continue;

                var ok = assignments.Any(a =>
                    a.FleetId is not null &&
                    string.Equals(a.FleetId, fleetId, StringComparison.OrdinalIgnoreCase) &&
                    fleetReq.Roles.Any(r =>
                        string.Equals(a.Role, r, StringComparison.OrdinalIgnoreCase)));

                if (ok) context.Succeed(requirement);
                continue;
            }
        }
    }

    private static string? GetRouteValue(AuthorizationHandlerContext context, string key)
    {
        // MVC controllers
        if (context.Resource is AuthorizationFilterContext mvc &&
            mvc.RouteData.Values.TryGetValue(key, out var v1))
            return v1?.ToString();

        // Minimal APIs / endpoint routing
        if (context.Resource is Microsoft.AspNetCore.Http.HttpContext http &&
            http.Request.RouteValues.TryGetValue(key, out var v2))
            return v2?.ToString();

        return null;
    }
}
