using Fair.Application.Auth;

namespace Fair.Infrastructure.Auth;

public sealed class InMemoryRoleAssignmentRepository : IRoleAssignmentRepository, IRoleAssignmentWriter
{
    private readonly List<RoleAssignment> _items = new();

    public Task<IReadOnlyList<RoleAssignment>> GetActiveByUserId(string userId, CancellationToken ct)
    {
        var active = _items
            .Where(x => x.UserId == userId && x.RevokedAtUtc is null)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<RoleAssignment>>(active);
    }

    public Task GrantAsync(string userId, string role, string? fleetId, CancellationToken ct)
    {
        // idempotent-ish: om samma aktiva role+fleet redan finns, gör inget
        var exists = _items.Any(x =>
            x.UserId == userId &&
            x.Role == role &&
            x.FleetId == fleetId &&
            x.RevokedAtUtc is null);

        if (!exists)
            _items.Add(new RoleAssignment(userId, role, fleetId, DateTimeOffset.UtcNow, null));

        return Task.CompletedTask;
    }
}
