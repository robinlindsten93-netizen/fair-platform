using Fair.Domain.Trips;

namespace Fair.Application.Trips.Quoting;

public interface IQuoteTokenService
{
    string CreateToken(TripQuote quote);
    bool TryParseToken(string token, out TripQuote quote);
}
