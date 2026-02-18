namespace Fair.Application.Dispatch;

public interface IDispatchOfferRepository
{
    Task AddManyAsync(IEnumerable<DispatchOfferDto> offers, CancellationToken ct);
    Task<IReadOnlyList<DispatchOfferDto>> GetPendingOffersForDriverAsync(string driverId, DateTimeOffset nowUtc, CancellationToken ct);
    Task<DispatchOfferDto?> GetByIdAsync(Guid offerId, CancellationToken ct);

    // Atomic accept: returns false if already accepted/expired/not found
    Task<bool> TryAcceptAsync(Guid offerId, string driverId, DateTimeOffset nowUtc, CancellationToken ct);

    Task ExpireOffersAsync(DateTimeOffset nowUtc, CancellationToken ct);
}
