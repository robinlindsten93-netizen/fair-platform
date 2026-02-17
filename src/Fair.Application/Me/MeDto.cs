namespace Fair.Application.Me;

public sealed record MeDto(
    string UserId,
    string? Phone,
    string[] Roles,
    string[] FleetScopes
);
