using Fair.Application.Dispatch;
using Fair.Application.Trips.Quoting;
using Fair.Domain.Trips;

namespace Fair.Application.Trips.RequestTrip;

public sealed class RequestTripHandler
{
    private readonly ITripRepository _repo;
    private readonly IQuoteTokenService _quoteTokens;
    private readonly CreateDispatchOffers _dispatch;

    public RequestTripHandler(ITripRepository repo, IQuoteTokenService quoteTokens, CreateDispatchOffers dispatch)
    {
        _repo = repo;
        _quoteTokens = quoteTokens;
        _dispatch = dispatch;
    }

    public async Task<RequestTripResult> HandleAsync(RequestTripRequest req, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        if (!_quoteTokens.TryParseToken(req.QuoteToken, out var quote))
            throw new UnauthorizedAccessException("invalid_quote_token");

        if (quote.IsExpired(now))
            throw new InvalidOperationException("quote_expired");

        var trip = await _repo.GetByIdAsync(req.TripId, ct);
        if (trip is null) throw new KeyNotFoundException("trip_not_found");

        // ✅ Idempotency: om den redan är Requested eller längre, gör inget.
        // (Vi kör INTE dispatch igen här)
        if (trip.Status is TripStatus.Requested
            or TripStatus.Accepted
            or TripStatus.Arriving
            or TripStatus.InProgress
            or TripStatus.Completed)
        {
            return new RequestTripResult(trip.Id, trip.Status);
        }

        // Endast Quoted kan requestas enligt domänen.
        if (trip.Status != TripStatus.Quoted)
            throw new InvalidOperationException("trip_not_requestable");

        var expectedVersion = trip.Version;

        // Domain transition: Quoted -> Requested
        trip.Request(now);

        var updated = await _repo.UpdateAsync(trip, expectedVersion, ct);
        if (!updated) throw new InvalidOperationException("concurrency_conflict");

        // ✅ Dispatch AFTER save (post-save version) och bara när vi faktiskt requestade
        await _dispatch.Handle(trip.Id, trip.Version, ct);

        return new RequestTripResult(trip.Id, trip.Status);
    }
}