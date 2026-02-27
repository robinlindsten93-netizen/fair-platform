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

    // ✅ Swagger kommer nu visa userId istället för fleetId
    public sealed record GrantRoleRequest(string UserId, string Role, string? FleetId = null);

    [HttpPost("grant")]
    public async Task<IActionResult> Grant([FromBody] GrantRoleRequest req, CancellationToken ct)
    {
        // PROD-SAFE: endpointen finns inte utanför dev
        if (!_env.IsDevelopment())
            return NotFound();

        if (string.IsNullOrWhiteSpace(req.UserId))
            return BadRequest(new { error = "UserId is required." });

        if (!Guid.TryParse(req.UserId, out var parsedUserId) || parsedUserId == Guid.Empty)
            return BadRequest(new { error = "UserId must be a non-empty GUID." });

        if (string.IsNullOrWhiteSpace(req.Role))
            return BadRequest(new { error = "Role is required." });

        // Validate role (tight allow-list)
        var role = req.Role.Trim().ToUpperInvariant();

        var allowed = new[]
        {
            Role.Rider, Role.Driver, Role.Owner, Role.FleetAdmin
        }
        .Select(x => x.Trim().ToUpperInvariant())
        .ToArray();

        if (!allowed.Contains(role))
            return BadRequest(new { error = $"Invalid role. Allowed: {string.Join(", ", allowed)}" });

        // FleetId är optional: validera endast om den skickas
        if (!string.IsNullOrWhiteSpace(req.FleetId) &&
            (!Guid.TryParse(req.FleetId, out var parsedFleetId) || parsedFleetId == Guid.Empty))
        {
            return BadRequest(new { error = "FleetId must be a non-empty GUID when provided." });
        }

        await _writer.GrantAsync(req.UserId, role, req.FleetId, ct);

        // Om du gav rollen till DIG SJÄLV: returnera uppdaterad /me (bra DX)
        var currentUserId =
            User.FindFirst("sub")?.Value ??
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrWhiteSpace(currentUserId) &&
            string.Equals(currentUserId, req.UserId, StringComparison.OrdinalIgnoreCase))
        {
            var me = await _getMe.Handle(User, ct);
            return Ok(me);
        }

        // Annars: returnera bara vad som gjordes
        return Ok(new
        {
            userId = req.UserId,
            role,
            fleetId = req.FleetId
        });
    }
}