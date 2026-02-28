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

        if (req.RiderId == Guid.Empty)
            throw new ArgumentException("rider_id_required", nameof(req.RiderId));

        if (!_quoteTokens.TryParseToken(req.QuoteToken, out var quote))
            throw new UnauthorizedAccessException("invalid_quote_token");

        if (quote.IsExpired(now))
            throw new InvalidOperationException("quote_expired");

        if (!Enum.IsDefined(typeof(TransportMode), req.Mode))
            throw new ArgumentOutOfRangeException(nameof(req.Mode), "invalid_transport_mode");

        var pickup = Location.Create(req.PickupLat, req.PickupLng);
        var dropoff = Location.Create(req.DropoffLat, req.DropoffLng);
        var mode = (TransportMode)req.Mode;

        // Skapa draft med samma "now"
        var trip = Trip.CreateDraft(req.RiderId, pickup, dropoff, mode, now);

        // Draft -> Quoted (skicka now för determinism)
        trip.ApplyQuote(quote, now);

        await _repo.AddAsync(trip, ct);

        // Returnera mer DX: tripId + status (Quoted)
        return new CreateTripResult(trip.Id, trip.Status);
    }
}