using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/auth/otp")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    // POST /api/v1/auth/otp/request
    [HttpPost("request")]
    public IActionResult RequestOtp([FromBody] OtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { error = "phone_required" });

        var code = "123456";

        return Ok(new
        {
            phone = request.Phone,
            dev_code = code
        });
    }

    // POST /api/v1/auth/otp/verify
    [HttpPost("verify")]
    public IActionResult VerifyOtp([FromBody] OtpVerifyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { error = "phone_and_code_required" });

        if (request.Code != "123456")
            return Unauthorized(new { error = "invalid_code" });

        var jwt = _config.GetSection("Jwt");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "usr_1"),
            new Claim("phone", request.Phone),
            new Claim(ClaimTypes.Role, "CUSTOMER"),
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            access_token = accessToken,
            refresh_token = "dev-refresh-token"
        });
    }

    public record OtpRequest(string Phone);
    public record OtpVerifyRequest(string Phone, string Code);
}

