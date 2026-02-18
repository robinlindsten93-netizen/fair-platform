using System.Security.Claims;

namespace Fair.Application.Dispatch;

public sealed class GetMyOffers
{
    private readonly IDispatchOfferRepository _offers;

    public GetMyOffers(IDispatchOfferRepository offers) => _offers = offers;

    public Task<IReadOnlyList<DispatchOfferDto>> Handle(ClaimsPrincipal user, CancellationToken ct)
    {
        var driverId =
            user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            user.FindFirst("sub")?.Value ??
            throw new InvalidOperationException("Missing user id claim (sub/nameidentifier).");

        var now = DateTimeOffset.UtcNow;
        return _offers.GetPendingOffersForDriverAsync(driverId, now, ct);
    }
}
