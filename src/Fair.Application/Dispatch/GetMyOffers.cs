using System.Security.Claims;

namespace Fair.Application.Dispatch;

public sealed class GetMyOffers
{
    private readonly IDispatchOfferRepository _offers;

    public GetMyOffers(IDispatchOfferRepository offers) => _offers = offers;

    public async Task<IReadOnlyList<DispatchOfferDto>> Handle(ClaimsPrincipal user, CancellationToken ct)
    {
        var sub =
            user.FindFirst("sub")?.Value ??
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            throw new InvalidOperationException("Missing user id claim (sub/nameidentifier).");

        if (!Guid.TryParse(sub, out var driverId) || driverId == Guid.Empty)
            throw new InvalidOperationException("Invalid user id claim (expected Guid).");

        var now = DateTimeOffset.UtcNow;

        // 🔎 DEV log: när det "slutar funka" är det nästan alltid fel driverId eller att TTL gått ut.
        Console.WriteLine($"[GetMyOffers] driverId={driverId} nowUtc={now:O}");

        var list = await _offers.GetPendingOffersForDriverAsync(driverId, now, ct);

        Console.WriteLine($"[GetMyOffers] pendingCount={list.Count}");

        return list;
    }
}