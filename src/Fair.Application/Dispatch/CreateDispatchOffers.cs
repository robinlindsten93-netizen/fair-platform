using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Fair.Application.Dispatch;

public sealed class CreateDispatchOffers
{
    private readonly IDriverAvailabilityQuery _drivers;
    private readonly IDispatchOfferRepository _offers;

    public CreateDispatchOffers(IDriverAvailabilityQuery drivers, IDispatchOfferRepository offers)
    {
        _drivers = drivers ?? throw new ArgumentNullException(nameof(drivers));
        _offers = offers ?? throw new ArgumentNullException(nameof(offers));
    }

    public async Task Handle(Guid tripId, int tripVersion, CancellationToken ct)
    {
        if (tripId == Guid.Empty) throw new ArgumentException("tripId cannot be empty.", nameof(tripId));
        if (tripVersion < 0) throw new ArgumentOutOfRangeException(nameof(tripVersion));

        var now = DateTimeOffset.UtcNow;

        // ✅ DEV: lång TTL så du slipper "funkade sen dog" under test
        var expiresAt = now.AddMinutes(60);

        // Hämta online drivers och materialisera direkt (stabilitet)
        var driverIds = (await _drivers.GetOnlineDriverIdsAsync(ct))
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        Console.WriteLine($"[CreateDispatchOffers] tripId={tripId} tripVersion={tripVersion} nowUtc={now:O} expiresAtUtc={expiresAt:O}");
        Console.WriteLine($"[CreateDispatchOffers] onlineDrivers={driverIds.Count}");

        // Ta första N
        var selected = driverIds.Take(10).ToList();
        Console.WriteLine($"[CreateDispatchOffers] selectedDrivers={selected.Count}");

        if (selected.Count == 0)
        {
            Console.WriteLine("[CreateDispatchOffers] no drivers online -> no offers created");
            return;
        }

        // Bygg offers som en LISTA (inte deferred IEnumerable)
        var offers = new List<DispatchOfferDto>(capacity: selected.Count);

        foreach (var driverId in selected)
        {
            var dto = new DispatchOfferDto(
                OfferId: Guid.NewGuid(),
                TripId: tripId,
                DriverId: driverId,
                TripVersion: tripVersion,
                CreatedAtUtc: now,
                ExpiresAtUtc: expiresAt,
                Status: "PENDING"
            );

            offers.Add(dto);

            Console.WriteLine($"[CreateDispatchOffers] +offer offerId={dto.OfferId} driverId={dto.DriverId} tripId={dto.TripId} expiresAtUtc={dto.ExpiresAtUtc:O}");
        }

        await _offers.AddManyAsync(offers, ct);

        Console.WriteLine($"[CreateDispatchOffers] saved offers={offers.Count}");
    }
}