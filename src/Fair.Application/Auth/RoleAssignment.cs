namespace Fair.Application.Auth;

public sealed record RoleAssignment(
    string UserId,
    string Role,
    string? FleetId,
    DateTimeOffset GrantedAtUtc,
    DateTimeOffset? RevokedAtUtc
);
