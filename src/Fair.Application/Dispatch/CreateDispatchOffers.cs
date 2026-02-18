namespace Fair.Application.Dispatch;

public sealed class CreateDispatchOffers
{
    private readonly IDriverAvailabilityQuery _drivers;
    private readonly IDispatchOfferRepository _offers;

    public CreateDispatchOffers(IDriverAvailabilityQuery drivers, IDispatchOfferRepository offers)
    {
        _drivers = drivers;
        _offers = offers;
    }

    public async Task Handle(Guid tripId, int tripVersion, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddSeconds(30); // v1: 30s offer TTL

        var onlineDrivers = await _drivers.GetOnlineDriverIdsAsync(ct);

        // v1 matching: broadcast till max 10 online drivers
        var selected = onlineDrivers.Take(10).ToArray();

        var offers = selected.Select(driverId => new DispatchOfferDto(
            OfferId: Guid.NewGuid(),
            TripId: tripId,
            DriverId: driverId,
            TripVersion: tripVersion,
            CreatedAtUtc: now,
            ExpiresAtUtc: expires,
            Status: "PENDING"
        ));

        await _offers.AddManyAsync(offers, ct);
    }
}
