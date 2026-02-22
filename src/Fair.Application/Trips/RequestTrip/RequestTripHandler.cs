using Fair.Application.Dispatch;
using Fair.Application.Trips.Quoting;

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

        var expectedVersion = trip.Version;

        // Domain transition
        trip.Request(now);

        var updated = await _repo.UpdateAsync(trip, expectedVersion, ct);
        if (!updated) throw new InvalidOperationException("concurrency_conflict");

        // Dispatch AFTER save
        await _dispatch.Handle(trip.Id, trip.Version, ct);

        return new RequestTripResult(trip.Id, trip.Status);
    }
}