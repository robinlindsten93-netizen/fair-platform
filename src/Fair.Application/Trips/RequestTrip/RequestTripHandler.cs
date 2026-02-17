using Fair.Application.Trips;
using Fair.Application.Trips.Quoting;
using Fair.Domain.Trips;

namespace Fair.Application.Trips.RequestTrip;

public sealed class RequestTripHandler
{
    private readonly ITripRepository _repo;
    private readonly IQuoteTokenService _quoteTokens;

    public RequestTripHandler(
        ITripRepository repo,
        IQuoteTokenService quoteTokens)
    {
        _repo = repo;
        _quoteTokens = quoteTokens;
    }

    public async Task<RequestTripResult> HandleAsync(
        RequestTripRequest req,
        CancellationToken ct = default)
    {
        if (req.TripId == Guid.Empty)
            throw new ArgumentException("TripId is required.", nameof(req.TripId));

        if (string.IsNullOrWhiteSpace(req.QuoteToken))
            throw new ArgumentException("QuoteToken is required.", nameof(req.QuoteToken));

        var trip = await _repo.GetByIdAsync(req.TripId, ct);
        if (trip is null)
            throw new KeyNotFoundException("trip_not_found");

        // ✅ Idempotens: redan Requested → OK
        if (trip.Status == TripStatus.Requested)
            return new RequestTripResult(trip.Id, trip.Status);

        // ❌ För sent i flödet
        if (trip.Status is TripStatus.Accepted
            or TripStatus.Arriving
            or TripStatus.InProgress
            or TripStatus.Completed)
            throw new InvalidOperationException("trip_not_requestable");

        // 🔐 Parse + validera quote-token
        if (!_quoteTokens.TryParseToken(req.QuoteToken, out var quote))
            throw new UnauthorizedAccessException("invalid_quote_token");

        var now = DateTimeOffset.UtcNow;

        // ⚡ Snabb UX-check (domänen dubbelkollar)
        if (quote.IsExpired(now))
            throw new InvalidOperationException("quote_expired");

        // 🧠 Domain operations
        trip.ApplyQuote(quote, now);
        trip.Request(now);

        // 🔒 Optimistic concurrency (VIKTIGT)
        var ok = await _repo.UpdateAsync(trip, trip.Version, ct);
        if (!ok)
            throw new InvalidOperationException("concurrency_conflict");

        return new RequestTripResult(trip.Id, trip.Status);
    }
}
