using Fair.Application.Trips.Quoting;
using Fair.Domain.Trips;

namespace Fair.Application.Trips.CreateTrip;

public sealed class CreateTripHandler
{
    private readonly ITripRepository _repo;
    private readonly IQuoteTokenService _quoteTokens;

    public CreateTripHandler(ITripRepository repo, IQuoteTokenService quoteTokens)
    {
        _repo = repo;
        _quoteTokens = quoteTokens;
    }

    public async Task<CreateTripResult> HandleAsync(CreateTripRequest req, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        if (!_quoteTokens.TryParseToken(req.QuoteToken, out var quote))
            throw new UnauthorizedAccessException("invalid_quote_token");

        if (quote.IsExpired(now))
            throw new InvalidOperationException("quote_expired");

        if (!Enum.IsDefined(typeof(TransportMode), req.Mode))
            throw new ArgumentOutOfRangeException(nameof(req.Mode), "invalid_transport_mode");

        var pickup = Location.Create(req.PickupLat, req.PickupLng);
        var dropoff = Location.Create(req.DropoffLat, req.DropoffLng);

        var mode = (TransportMode)req.Mode;

        var trip = Trip.CreateDraft(req.RiderId, pickup, dropoff, mode);

        // Viktigt: bara quote här. Request sker i /request-endpointen.
        trip.ApplyQuote(quote);

        await _repo.AddAsync(trip, ct);
        return new CreateTripResult(trip.Id);
    }
}