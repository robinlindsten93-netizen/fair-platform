namespace Fair.Application.Auth;

public interface IRoleAssignmentWriter
{
    Task GrantAsync(string userId, string role, string? fleetId, CancellationToken ct);
}
