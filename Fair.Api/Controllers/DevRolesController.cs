using Fair.Application.Auth;
using Fair.Application.Me;
using Fair.Domain.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/dev/roles")]
[Authorize]
public sealed class DevRolesController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly IRoleAssignmentWriter _writer;
    private readonly GetMe _getMe;

    public DevRolesController(IWebHostEnvironment env, IRoleAssignmentWriter writer, GetMe getMe)
    {
        _env = env;
        _writer = writer;
        _getMe = getMe;
    }

    public sealed record GrantRoleRequest(string Role, string? FleetId);

    [HttpPost("grant")]
    public async Task<IActionResult> Grant([FromBody] GrantRoleRequest req, CancellationToken ct)
    {
        // PROD-SAFE: endpointen finns inte utanför dev
        if (!_env.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(req.Role))
            return BadRequest(new { error = "Role is required." });

        // Validate role (tight allow-list)
        var role = req.Role.Trim().ToUpperInvariant();
        var allowed = new[]
        {
            Role.Rider, Role.Driver, Role.Owner, Role.FleetAdmin
        };

        if (!allowed.Contains(role))
            return BadRequest(new { error = $"Invalid role. Allowed: {string.Join(", ", allowed)}" });

        var userId =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("sub")?.Value ??
            throw new InvalidOperationException("Missing user id claim (sub/nameidentifier).");

        await _writer.GrantAsync(userId, role, req.FleetId, ct);

        // Returnera uppdaterad /me direkt (bra DX)
        var me = await _getMe.Handle(User, ct);
        return Ok(me);
    }
}
