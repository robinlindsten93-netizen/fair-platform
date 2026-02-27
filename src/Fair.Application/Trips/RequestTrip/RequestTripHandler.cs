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

        // ✅ Idempotency: redan requestad (eller längre fram) -> returnera utan att ändra
        if (trip.Status is TripStatus.Requested
            or TripStatus.Accepted
            or TripStatus.Arriving
            or TripStatus.InProgress
            or TripStatus.Completed)
        {
            return new RequestTripResult(trip.Id, trip.Status);
        }

        var expectedVersion = trip.Version;

        // ✅ Apply quote bara när domänen tillåter
        if (trip.Status is TripStatus.Draft or TripStatus.Quoted)
        {
            trip.ApplyQuote(quote, now);
        }

        // ✅ Quoted -> Requested
        trip.Request(now);

        var updated = await _repo.UpdateAsync(trip, expectedVersion, ct);
        if (!updated) throw new InvalidOperationException("concurrency_conflict");

        // Dispatch AFTER save (use post-save version)
        await _dispatch.Handle(trip.Id, trip.Version, ct);

        return new RequestTripResult(trip.Id, trip.Status);
    }
}