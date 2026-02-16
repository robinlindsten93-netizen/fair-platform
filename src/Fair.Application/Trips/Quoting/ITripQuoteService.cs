using Fair.Domain.Trips;

namespace Fair.Application.Trips.Quoting;

public interface ITripQuoteService
{
    TripQuote CreateQuote(
        Location pickup,
        Location dropoff,
        TransportMode mode,
        DateTimeOffset nowUtc);
}
