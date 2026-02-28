using Fair.Application.Trips;
using Fair.Application.Trips.Queries;
using Fair.Domain.Trips;

namespace Fair.Infrastructure.Trips.Queries;

public sealed class InMemoryTripReadRepository : ITripReadRepository
{
    private readonly ITripRepository _trips;

    public InMemoryTripReadRepository(ITripRepository trips)
    {
        _trips = trips ?? throw new ArgumentNullException(nameof(trips));
    }

    // =========================
    // GET BY ID
    // =========================
    public async Task<TripReadDto?> GetByIdAsync(Guid tripId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var trip = await _trips.GetByIdAsync(tripId, ct);
        return trip is null ? null : Map(trip);
    }

    // =========================
    // GET BY RIDER
    // =========================
    public async Task<IReadOnlyList<TripReadDto>> GetByRiderAsync(Guid riderId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (_trips is not ITripListSource listSource)
            return Array.Empty<TripReadDto>();

        var all = await listSource.ListAllAsync(ct);

        return all
            .Where(t => t.RiderId == riderId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(Map)
            .ToList()
            .AsReadOnly();
    }

    // =========================
    // GET BY DRIVER
    // =========================
    public async Task<IReadOnlyList<TripReadDto>> GetByDriverAsync(Guid driverId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (_trips is not ITripListSource listSource)
            return Array.Empty<TripReadDto>();

        var all = await listSource.ListAllAsync(ct);

        return all
            .Where(t => t.DriverId.HasValue && t.DriverId.Value == driverId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(Map)
            .ToList()
            .AsReadOnly();
    }

    // =========================
    // MAPPING
    // =========================
    private static TripReadDto Map(Trip t) => new(
        Id: t.Id,
        RiderId: t.RiderId,
        Mode: (int)t.Mode,
        Status: t.Status.ToString(),

        PickupLat: t.Pickup.Latitude,
        PickupLng: t.Pickup.Longitude,
        DropoffLat: t.Dropoff.Latitude,
        DropoffLng: t.Dropoff.Longitude,

        Quote: t.Quote is null
            ? null
            : new TripQuoteReadDto(
                EstimatedDistanceMeters: t.Quote.EstimatedDistanceMeters,
                EstimatedDurationSeconds: t.Quote.EstimatedDurationSeconds,
                Price: new MoneyReadDto(
                    t.Quote.Price.Amount,
                    t.Quote.Price.Currency),
                ExpiresAtUtc: t.Quote.ExpiresAtUtc,
                SurgeMultiplier: t.Quote.SurgeMultiplier
            ),

        DriverId: t.DriverId,
        VehicleId: t.VehicleId,
        CreatedAtUtc: t.CreatedAtUtc,
        UpdatedAtUtc: t.UpdatedAtUtc,
        Version: t.Version
    );
}