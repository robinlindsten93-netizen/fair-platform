using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Fair.Application.Abstractions;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/auth/otp")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwt;

    public AuthController(IJwtTokenService jwt)
    {
        _jwt = jwt;
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

        // ✅ Clean architecture: token skapas via service
        var accessToken = _jwt.CreateToken(
            userId: Guid.NewGuid(),
            role: "CUSTOMER"
        );

        return Ok(new
        {
            access_token = accessToken,
            refresh_token = "dev-refresh-token"
        });
    }

    public record OtpRequest(string Phone);
    public record OtpVerifyRequest(string Phone, string Code);
}

