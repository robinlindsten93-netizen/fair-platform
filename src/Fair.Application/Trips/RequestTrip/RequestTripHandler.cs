using Fair.Application.Dispatch;
using Fair.Application.Trips.Guards;
using Fair.Application.Trips.Quoting;

namespace Fair.Application.Trips.RequestTrip;

public sealed class RequestTripHandler
{
    private readonly ITripRepository _repo;
    private readonly IQuoteTokenService _quoteTokens;
    private readonly CreateDispatchOffers _dispatch;
    private readonly ActiveTripGuard _activeTripGuard;

    public RequestTripHandler(
        ITripRepository repo,
        IQuoteTokenService quoteTokens,
        CreateDispatchOffers dispatch,
        ActiveTripGuard activeTripGuard)
    {
        _repo = repo;
        _quoteTokens = quoteTokens;
        _dispatch = dispatch;
        _activeTripGuard = activeTripGuard;
    }

    public async Task<RequestTripResult> HandleAsync(RequestTripRequest req, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        if (!_quoteTokens.TryParseToken(req.QuoteToken, out var quote))
            throw new UnauthorizedAccessException("invalid_quote_token");

        if (quote.IsExpired(now))
            throw new InvalidOperationException("quote_expired");

        var trip = await _repo.GetByIdAsync(req.TripId, ct);
        if (trip is null)
            throw new KeyNotFoundException("trip_not_found");

        // ✅ Guard: stoppa rider från att requesta om den redan har annan aktiv trip
        await _activeTripGuard.EnsureRiderHasNoOtherActiveTripAsync(trip.RiderId, trip.Id, ct);

        // snapshot version innan mutation
        var expectedVersion = trip.Version;

        // Draft/Quoted -> Quoted
        trip.ApplyQuote(quote, now);

        // Quoted -> Requested
        trip.Request(now);

        var updated = await _repo.UpdateAsync(trip, expectedVersion, ct);
        if (!updated)
            throw new InvalidOperationException("concurrency_conflict");

        // Dispatch AFTER save (använd post-save version)
        await _dispatch.Handle(trip.Id, trip.Version, ct);

        return new RequestTripResult(trip.Id, trip.Status);
    }
}