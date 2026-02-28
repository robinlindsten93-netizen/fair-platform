using Fair.Application.Dispatch;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fair.Api.Controllers;

[ApiController]
[Route("api/v1/dispatch")]
[Authorize]
public sealed class DispatchController : ControllerBase
{
    // =========================
    // GET MY OFFERS (Driver inbox)
    // =========================
    [HttpGet("offers")]
    [Authorize(Policy = "Driver")]
    [ProducesResponseType(typeof(IReadOnlyList<DispatchOfferDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<DispatchOfferDto>>> GetMyOffers(
        [FromServices] GetMyOffers uc,
        CancellationToken ct)
    {
        var result = await uc.Handle(User, ct);
        return Ok(result);
    }

    // =========================
    // ACCEPT OFFER (Driver)
    // =========================
    public sealed record AcceptOfferBody(Guid OfferId);

    [HttpPost("offers/accept")]
    [Authorize(Policy = "Driver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Accept(
        [FromBody] AcceptOfferBody body,
        [FromServices] AcceptDispatchOffer uc,
        CancellationToken ct)
    {
        if (body is null || body.OfferId == Guid.Empty)
            return BadRequest(new { error = "offer_id_required" });

        var ok = await uc.Handle(User, body.OfferId, ct);

        // ✅ idempotent-friendly response
        return Ok(new
        {
            accepted = ok
        });
    }
}