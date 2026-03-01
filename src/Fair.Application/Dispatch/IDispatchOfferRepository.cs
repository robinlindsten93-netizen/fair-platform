namespace Fair.Application.Dispatch;

public interface IDispatchOfferRepository
{
    // =========================
    // CREATE (batch)
    // =========================
    Task AddManyAsync(IEnumerable<DispatchOfferDto> offers, CancellationToken ct);

    // =========================
    // QUERY
    // =========================
    Task<IReadOnlyList<DispatchOfferDto>> GetPendingOffersForDriverAsync(
        Guid driverId,
        DateTimeOffset nowUtc,
        CancellationToken ct);

    Task<DispatchOfferDto?> GetByIdAsync(Guid offerId, CancellationToken ct);

    /// <summary>
    /// Returns the set of drivers that already have an offer for this trip + version.
    /// Used for wave dispatch dedupe.
    /// </summary>
    Task<IReadOnlySet<Guid>> GetOfferedDriverIdsAsync(
        Guid tripId,
        int tripVersion,
        CancellationToken ct);

    // =========================
    // IDEMPOTENCY (v2)
    // =========================
    Task<bool> ExistsForTripVersionAsync(
        Guid tripId,
        int tripVersion,
        CancellationToken ct);

    // =========================
    // ACCEPT
    // =========================
    Task<bool> TryAcceptAsync(
        Guid offerId,
        Guid driverId,
        DateTimeOffset nowUtc,
        CancellationToken ct);

    // =========================
    // EXPIRY
    // =========================
    Task ExpireOffersAsync(DateTimeOffset nowUtc, CancellationToken ct);
}