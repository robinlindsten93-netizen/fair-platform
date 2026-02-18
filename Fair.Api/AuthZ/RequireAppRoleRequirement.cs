using Microsoft.AspNetCore.Authorization;

namespace Fair.Api.AuthZ;

public sealed class RequireAppRoleRequirement : IAuthorizationRequirement
{
    public string Role { get; }
    public RequireAppRoleRequirement(string role) => Role = role;
}
