using Microsoft.AspNetCore.Mvc;
using Fair.Application.Abstractions;
using System.Collections.Concurrent;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/auth/otp")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwt;

    // DEV-ONLY: stabil mapping phone -> userId så roller inte "försvinner"
    private static readonly ConcurrentDictionary<string, Guid> _usersByPhone = new();

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

        var normalizedPhone = NormalizePhone(request.Phone);

        var code = "123456";

        return Ok(new
        {
            phone = normalizedPhone,
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

        var normalizedPhone = NormalizePhone(request.Phone);

        // Samma telefonnummer -> samma userId varje gång
        var userId = _usersByPhone.GetOrAdd(normalizedPhone, _ => Guid.NewGuid());

        // Behåll enkel dev-roll i token.
        // Policies hämtar riktiga app-roller från role assignment repo.
        var accessToken = _jwt.CreateToken(
            userId: userId,
            role: "CUSTOMER"
        );

        return Ok(new
        {
            access_token = accessToken,
            refresh_token = "dev-refresh-token",
            user_id = userId,
            phone = normalizedPhone
        });
    }

    private static string NormalizePhone(string phone)
        => phone.Trim();

    public record OtpRequest(string Phone);
    public record OtpVerifyRequest(string Phone, string Code);
}