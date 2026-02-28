namespace Fair.Application.Dispatch;

public sealed class CreateDispatchOffers
{
    private readonly IDriverAvailabilityQuery _drivers;
    private readonly IDispatchOfferRepository _offers;

    public CreateDispatchOffers(
        IDriverAvailabilityQuery drivers,
        IDispatchOfferRepository offers)
    {
        _drivers = drivers;
        _offers = offers;
    }

    public async Task Handle(Guid tripId, int tripVersion, CancellationToken ct)
    {
        if (tripId == Guid.Empty) throw new ArgumentException("tripId cannot be empty.", nameof(tripId));
        if (tripVersion < 0) throw new ArgumentOutOfRangeException(nameof(tripVersion));

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(60);

        var driverIds = (await _drivers.GetOnlineDriverIdsAsync(ct))
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Take(10)
            .ToList();

        if (driverIds.Count == 0)
            return;

        var offers = new List<DispatchOfferDto>(driverIds.Count);

        foreach (var driverId in driverIds)
        {
            // ✅ deterministiskt -> idempotent
            var offerId = DeterministicGuid.ForOffer(tripId, tripVersion, driverId);

            offers.Add(new DispatchOfferDto(
                OfferId: offerId,
                TripId: tripId,
                DriverId: driverId,
                TripVersion: tripVersion,
                CreatedAtUtc: now,
                ExpiresAtUtc: expiresAt,
                Status: "PENDING"
            ));
        }

        await _offers.AddManyAsync(offers, ct);
    }
}