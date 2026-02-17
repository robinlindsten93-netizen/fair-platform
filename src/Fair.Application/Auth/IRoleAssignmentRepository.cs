namespace Fair.Application.Auth;

public interface IRoleAssignmentRepository
{
    Task<IReadOnlyList<RoleAssignment>> GetActiveByUserId(string userId, CancellationToken ct);
}
