using Fair.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Fair.Api.AuthZ;

public sealed class RoleAuthorizationHandler : IAuthorizationHandler
{
    private readonly IRoleAssignmentRepository _rolesRepo;
    private readonly ILogger<RoleAuthorizationHandler> _log;

    public RoleAuthorizationHandler(
        IRoleAssignmentRepository rolesRepo,
        ILogger<RoleAuthorizationHandler> log)
    {
        _rolesRepo = rolesRepo;
        _log = log;
    }

    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var userId =
            context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            context.User.FindFirst("sub")?.Value;

        _log.LogInformation("AuthZ start. userId={UserId}, requirements={Requirements}",
            userId ?? "<null>",
            string.Join(", ", context.Requirements.Select(r => r.GetType().Name)));

        if (string.IsNullOrWhiteSpace(userId))
        {
            _log.LogWarning("AuthZ abort: missing userId claim.");
            return;
        }

        var assignments = await _rolesRepo.GetActiveByUserId(userId, CancellationToken.None);

        _log.LogInformation("AuthZ assignments for userId={UserId}: {Assignments}",
            userId,
            string.Join(" | ", assignments.Select(a => $"role={a.Role}, fleet={a.FleetId ?? "<null>"}")));

        foreach (var requirement in context.Requirements)
        {
            if (requirement is RequireAppRoleRequirement appRoleReq)
            {
                var ok = assignments.Any(a =>
                    a.FleetId is null &&
                    string.Equals(a.Role, appRoleReq.Role, StringComparison.OrdinalIgnoreCase));

                _log.LogInformation(
                    "AuthZ app-role check userId={UserId}, requiredRole={RequiredRole}, ok={Ok}",
                    userId, appRoleReq.Role, ok);

                if (ok)
                    context.Succeed(requirement);

                continue;
            }

            if (requirement is RequireFleetRoleRequirement fleetReq)
            {
                var fleetId = GetRouteValue(context, "fleetId");
                if (string.IsNullOrWhiteSpace(fleetId))
                {
                    _log.LogWarning("AuthZ fleet-role check skipped: missing fleetId route value.");
                    continue;
                }

                var ok = assignments.Any(a =>
                    a.FleetId is not null &&
                    string.Equals(a.FleetId, fleetId, StringComparison.OrdinalIgnoreCase) &&
                    fleetReq.Roles.Any(r =>
                        string.Equals(a.Role, r, StringComparison.OrdinalIgnoreCase)));

                _log.LogInformation(
                    "AuthZ fleet-role check userId={UserId}, fleetId={FleetId}, requiredRoles={RequiredRoles}, ok={Ok}",
                    userId,
                    fleetId,
                    string.Join(", ", fleetReq.Roles),
                    ok);

                if (ok)
                    context.Succeed(requirement);

                continue;
            }
        }
    }

    private static string? GetRouteValue(AuthorizationHandlerContext context, string key)
    {
        if (context.Resource is AuthorizationFilterContext mvc &&
            mvc.RouteData.Values.TryGetValue(key, out var v1))
            return v1?.ToString();

        if (context.Resource is Microsoft.AspNetCore.Http.HttpContext http &&
            http.Request.RouteValues.TryGetValue(key, out var v2))
            return v2?.ToString();

        return null;
    }
}