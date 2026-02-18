using Microsoft.AspNetCore.Authorization;

namespace Fair.Api.AuthZ;

public sealed class RequireFleetRoleRequirement : IAuthorizationRequirement
{
    public string[] Roles { get; }

    public RequireFleetRoleRequirement(params string[] roles)
        => Roles = roles;
}
