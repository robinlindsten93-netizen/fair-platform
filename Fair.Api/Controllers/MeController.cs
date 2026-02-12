using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class MeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

        return Ok(new
        {
            user = new
            {
                id = User.FindFirst("sub")?.Value,
                phone = User.FindFirst("phone")?.Value
            },
            roles = roles,
            fleets = Array.Empty<object>()
        });
    }
}
